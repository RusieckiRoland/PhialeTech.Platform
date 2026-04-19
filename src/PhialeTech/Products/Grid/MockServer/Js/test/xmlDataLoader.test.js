import test from 'node:test';
import assert from 'node:assert/strict';
import { loadDefaultRecords } from '../src/xmlDataLoader.js';

test('loadDefaultRecords loads GIS dataset with typed values', () => {
  const records = loadDefaultRecords();

  assert.equal(records.length, 530);
  assert.equal(records[0].Category, 'Parcel');
  assert.equal(typeof records[0].AreaSquareMeters, 'number');
  assert.equal(typeof records[0].Visible, 'boolean');
  assert.match(records[0].LastInspection, /^\d{4}-\d{2}-\d{2}T/);
});
