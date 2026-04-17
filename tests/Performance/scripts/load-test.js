import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    stages: [
        { duration: '5s', target: 10 },
        { duration: '10s', target: 10 },
        { duration: '5s', target: 0 }, 
    ],
};

export default function () {
    const res = http.get(`${__ENV.BASE_URL}/api/students`);
    check(res, { 'load: status is 200': (r) => r.status === 200 });
    sleep(1);
}