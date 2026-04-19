import test from 'node:test';
import assert from 'node:assert/strict';
import { executeGroupedQuery, executeQuery } from '../src/gridQueryEngine.js';
import { loadDefaultRecords } from '../src/xmlDataLoader.js';

const records = loadDefaultRecords();

test('executeQuery applies filter, sort and summary', () => {
  const result = executeQuery(records, {
    offset: 0,
    size: 5,
    sorts: [{ columnId: 'AreaSquareMeters', direction: 'Descending' }],
    filterGroup: {
      logicalOperator: 'And',
      filters: [{ columnId: 'Municipality', operator: 'Equals', value: 'Wroclaw' }]
    },
    summaries: [{ columnId: 'AreaSquareMeters', type: 'Sum' }]
  });

  assert.equal(result.returnedCount, 5);
  assert.ok(result.totalCount > 5);
  assert.equal(result.items[0].Municipality, 'Wroclaw');
  assert.ok(result.items[0].AreaSquareMeters >= result.items[1].AreaSquareMeters);
  assert.ok(result.summary['AreaSquareMeters:Sum'] > 0);
});

test('executeGroupedQuery respects collapsed group ids', () => {
  const preview = executeGroupedQuery(records, {
    offset: 0,
    size: 20,
    groups: [{ columnId: 'Category', direction: 'Ascending' }]
  });

  const firstGroupId = preview.groupIds[0];
  const collapsed = executeGroupedQuery(records, {
    offset: 0,
    size: 20,
    groups: [{ columnId: 'Category', direction: 'Ascending' }],
    collapsedGroupIds: [firstGroupId]
  });

  assert.equal(collapsed.rows[0].kind, 'GroupHeader');
  assert.equal(collapsed.rows[0].groupId, firstGroupId);
  assert.equal(collapsed.rows[0].isExpanded, false);
});

test('executeQuery rejects custom summary', () => {
  assert.throws(() => executeQuery(records, {
    offset: 0,
    size: 10,
    summaries: [{ columnId: 'AreaSquareMeters', type: 'Custom' }]
  }), /Custom summaries/);
});
