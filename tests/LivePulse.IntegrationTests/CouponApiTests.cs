using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LivePulse.Application.Coupons;
using LivePulse.Application.Matches;
using LivePulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LivePulse.IntegrationTests;

[Collection(ApiCollection.Name)]
public class CouponApiTests(LivePulseApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task PlacingCoupon_ReturnsCreated_AndWritesOutboxMessage()
    {
        var match = await GetFirstMatchAsync();

        var response = await PostCouponAsync(NewKey(), match, match.Odds.Home);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var coupon = await response.Content.ReadFromJsonAsync<CouponDto>(ApiJson.Options);
        coupon!.TotalOdds.Should().Be(match.Odds.Home);
        (await OutboxMessageCountAsync(coupon.Id)).Should().Be(1);
    }

    [Fact]
    public async Task PlacingCoupon_WithOutdatedOdds_ReturnsConflict_AndSavesNothing()
    {
        var match = await GetFirstMatchAsync();
        var key = NewKey();

        var response = await PostCouponAsync(key, match, match.Odds.Home + 1m);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("code").GetString().Should().Be("coupon.odds_changed");
        problem.RootElement.GetProperty("changes")[0].GetProperty("currentOdds").GetProperty("home").GetDecimal()
            .Should().Be(match.Odds.Home);
        (await CouponCountAsync(key)).Should().Be(0);
    }

    [Fact]
    public async Task TwoParallelRequests_WithSameIdempotencyKey_CreateOneCoupon()
    {
        var match = await GetFirstMatchAsync();
        var key = NewKey();

        var responses = await Task.WhenAll(
            PostCouponAsync(key, match, match.Odds.Home),
            PostCouponAsync(key, match, match.Odds.Home));

        responses.Select(r => r.StatusCode).Should().BeEquivalentTo([HttpStatusCode.Created, HttpStatusCode.OK]);
        var coupons = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<CouponDto>(ApiJson.Options)));
        coupons[0]!.Id.Should().Be(coupons[1]!.Id);
        (await CouponCountAsync(key)).Should().Be(1);
    }

    private static string NewKey() => Guid.NewGuid().ToString();

    private async Task<MatchDto> GetFirstMatchAsync()
    {
        var matches = await _client.GetFromJsonAsync<List<MatchDto>>("/api/matches", ApiJson.Options);
        return matches!.First();
    }

    private Task<HttpResponseMessage> PostCouponAsync(string key, MatchDto match, decimal homeOdds)
    {
        var body = new
        {
            stake = 10,
            selections = new[] { new { matchId = match.Id, pick = "Home", odds = homeOdds } }
        };
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/coupons")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("Idempotency-Key", key);
        return _client.SendAsync(request);
    }

    private async Task<int> CouponCountAsync(string key)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LivePulseDbContext>();
        return await dbContext.Coupons.CountAsync(c => c.IdempotencyKey == key);
    }

    private async Task<int> OutboxMessageCountAsync(Guid couponId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LivePulseDbContext>();
        return await dbContext.OutboxMessages
            .CountAsync(m => m.Type == "CouponPlacedMessage" && m.Payload.Contains(couponId.ToString()));
    }
}
