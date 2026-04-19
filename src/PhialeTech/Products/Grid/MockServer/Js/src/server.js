import http from 'node:http';
import { gridColumns } from './columns.js';
import { executeGroupedQuery, executeQuery } from './gridQueryEngine.js';
import { loadDefaultRecords } from './xmlDataLoader.js';

export function createServer(options = {}) {
  const records = options.records ?? loadDefaultRecords();

  return http.createServer(async (request, response) => {
    try {
      if (request.method === 'GET' && request.url === '/api/phialegrid/health') {
        return writeJson(response, 200, {
          status: 'ok',
          service: 'PhialeGrid.MockServer.Js'
        });
      }

      if (request.method === 'GET' && request.url === '/api/phialegrid/schema') {
        return writeJson(response, 200, {
          totalRecordCount: records.length,
          columns: gridColumns
        });
      }

      if (request.method === 'POST' && request.url === '/api/phialegrid/query') {
        const body = await readJsonBody(request);
        return writeJson(response, 200, executeQuery(records, body));
      }

      if (request.method === 'POST' && request.url === '/api/phialegrid/grouped-query') {
        const body = await readJsonBody(request);
        return writeJson(response, 200, executeGroupedQuery(records, body));
      }

      return writeJson(response, 404, {
        error: 'Not found'
      });
    } catch (error) {
      return writeJson(response, 400, {
        error: error instanceof Error ? error.message : String(error)
      });
    }
  });
}

async function readJsonBody(request) {
  const chunks = [];
  for await (const chunk of request) {
    chunks.push(chunk);
  }

  if (chunks.length === 0) {
    return {};
  }

  const raw = Buffer.concat(chunks).toString('utf8');
  return raw.trim().length === 0 ? {} : JSON.parse(raw);
}

function writeJson(response, statusCode, payload) {
  const body = JSON.stringify(payload);
  response.statusCode = statusCode;
  response.setHeader('Content-Type', 'application/json; charset=utf-8');
  response.end(body);
}
