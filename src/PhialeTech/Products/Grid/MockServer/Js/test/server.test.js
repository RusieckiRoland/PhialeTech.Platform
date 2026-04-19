import test from 'node:test';
import assert from 'node:assert/strict';
import { once } from 'node:events';
import { createServer } from '../src/server.js';

test('HTTP server exposes health, schema, query and grouped-query endpoints', async () => {
  const server = createServer();
  server.listen(0);
  await once(server, 'listening');

  const { port } = server.address();
  const baseUrl = `http://127.0.0.1:${port}`;

  try {
    let response = await fetch(`${baseUrl}/api/phialegrid/health`);
    let payload = await response.json();
    assert.equal(response.status, 200);
    assert.equal(payload.status, 'ok');

    response = await fetch(`${baseUrl}/api/phialegrid/schema`);
    payload = await response.json();
    assert.equal(payload.totalRecordCount, 530);
    assert.equal(payload.columns.length, 12);

    response = await fetch(`${baseUrl}/api/phialegrid/query`, {
      method: 'POST',
      headers: { 'content-type': 'application/json' },
      body: JSON.stringify({
        offset: 0,
        size: 3,
        filterGroup: {
          logicalOperator: 'And',
          filters: [{ columnId: 'District', operator: 'Equals', value: 'Oliwa' }]
        }
      })
    });
    payload = await response.json();
    assert.equal(response.status, 200);
    assert.equal(payload.returnedCount, 3);
    assert.equal(payload.items[0].District, 'Oliwa');

    response = await fetch(`${baseUrl}/api/phialegrid/grouped-query`, {
      method: 'POST',
      headers: { 'content-type': 'application/json' },
      body: JSON.stringify({
        offset: 0,
        size: 10,
        groups: [{ columnId: 'Category', direction: 'Ascending' }]
      })
    });
    payload = await response.json();
    assert.equal(response.status, 200);
    assert.ok(payload.groupIds.length > 0);
    assert.equal(payload.rows[0].kind, 'GroupHeader');
  } finally {
    server.close();
    await once(server, 'close');
  }
});
