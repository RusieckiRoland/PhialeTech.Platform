import { createServer } from './server.js';

const port = Number.parseInt(process.env.PORT ?? '5080', 10);
const server = createServer();

server.listen(port, () => {
  console.log(`PhialeGrid.MockServer.Js listening on http://localhost:${port}`);
});
