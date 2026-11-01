// Нагрузочный тест (k6). Запуск: см. README, раздел "Нагрузочный тест".
// Профиль по умолчанию — «средний день» x ~2.5 для записи (30 rps) и читающая нагрузка 200 rps;
// профиль «пик» (Middle+): WRITE_RPS=120 READ_RPS=600.
import http from 'k6/http';
import { check } from 'k6';

const BASE = __ENV.BASE_URL || 'http://localhost:8080';
const READ_RPS = Number(__ENV.READ_RPS || 200);
const WRITE_RPS = Number(__ENV.WRITE_RPS || 30);
const DURATION = __ENV.DURATION || '2m';

export const options = {
  scenarios: {
    readers: { executor: 'constant-arrival-rate', exec: 'read', rate: READ_RPS, timeUnit: '1s',
               duration: DURATION, preAllocatedVUs: 50, maxVUs: 600 },
    writers: { executor: 'constant-arrival-rate', exec: 'write', rate: WRITE_RPS, timeUnit: '1s',
               duration: DURATION, preAllocatedVUs: 20, maxVUs: 300 },
  },
  thresholds: {
    'http_req_failed{scenario:readers}': ['rate<0.01'],
    'http_req_failed{scenario:writers}': ['rate<0.01'],
    'http_req_duration{scenario:readers}': ['p(95)<300'],
    'http_req_duration{scenario:writers}': ['p(95)<800'],
  },
};

const GQL = `query($s: CommentSortBy!, $d: Boolean!, $p: Int!) {
  comments(sortBy: $s, desc: $d, page: $p) {
    totalCount items { id userName email textHtml createdAt descendants { id parentId textHtml } } } }`;

function gql(sort, desc, page) {
  return http.post(`${BASE}/graphql`, JSON.stringify({ query: GQL, variables: { s: sort, d: desc, p: page } }),
    { headers: { 'Content-Type': 'application/json' }, tags: { name: `gql_${sort}` } });
}

function createComment(parentId) {
  const captcha = http.get(`${BASE}/api/captcha`, { tags: { name: 'captcha' } }).json('id');
  const body = {
    userName: `load${__VU}x${__ITER}`,
    email: `load${__VU}@example.com`,
    text: `Load test <strong>message</strong> ${Date.now()}`,
    captchaId: captcha,
    captchaAnswer: 'TEST',
  };
  if (parentId) body.parentId = parentId;
  return http.post(`${BASE}/api/comments`, body, { tags: { name: 'create_comment' } });
}

export function setup() {
  // Гарантируем, что есть к чему отвечать.
  let items = gql('CREATED_AT', true, 1).json('data.comments.items') || [];
  for (let i = items.length; i < 5; i++) createComment(null);
  items = gql('CREATED_AT', true, 1).json('data.comments.items') || [];
  return { ids: items.map((x) => x.id) };
}

export function read() {
  const r = Math.random();
  let res;
  if (r < 0.70) res = gql('CREATED_AT', true, 1);                                   // главная страница
  else if (r < 0.85) res = gql('USER_NAME', Math.random() < 0.5, 1);                // сортировка по имени
  else if (r < 0.95) res = gql('CREATED_AT', true, 1 + Math.floor(Math.random() * 5)); // пагинация
  else res = http.get(`${BASE}/api/search?q=load`, { tags: { name: 'search' } });   // поиск
  check(res, { 'read ok': (x) => x.status === 200 });
}

export function write(data) {
  const reply = Math.random() < 0.3 && data.ids.length > 0;
  const parent = reply ? data.ids[Math.floor(Math.random() * data.ids.length)] : null;
  check(createComment(parent), { 'created': (x) => x.status === 201 });
}
