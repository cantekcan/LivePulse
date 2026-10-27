import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8085';

export const options = {
  stages: [
    { duration: '10s', target: 20 },
    { duration: '30s', target: 20 },
    { duration: '10s', target: 0 },
  ],
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<500'],
  },
};

// A 409 is a normal answer when the odds moved, so it must not count as a failed request.
http.setResponseCallback(http.expectedStatuses({ min: 200, max: 299 }, 409));

export default function () {
  const matchesResponse = http.get(`${BASE_URL}/api/matches`);
  check(matchesResponse, { 'matches listed': (r) => r.status === 200 });

  const matches = matchesResponse.json();
  if (!matches || matches.length === 0) {
    sleep(1);
    return;
  }

  const match = matches[Math.floor(Math.random() * matches.length)];
  const body = JSON.stringify({
    stake: 10,
    selections: [{ matchId: match.id, pick: 'Home', odds: match.odds.home }],
  });

  const couponResponse = http.post(`${BASE_URL}/api/coupons`, body, {
    headers: {
      'Content-Type': 'application/json',
      'Idempotency-Key': `${__VU}-${__ITER}-${Date.now()}`,
    },
  });
  check(couponResponse, { 'coupon accepted or odds changed': (r) => r.status === 201 || r.status === 409 });

  sleep(1);
}
