import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    vus: 1,
    duration: '5s',
};

export default function () {
    const res = http.get(`${__ENV.BASE_URL}/api/students`);
    check(res, { 'smoke: status is 200': (r) => r.status === 200 });
    sleep(1);
}