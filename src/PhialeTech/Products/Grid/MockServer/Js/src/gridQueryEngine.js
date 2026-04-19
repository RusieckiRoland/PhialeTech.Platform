import { gridColumnsById } from './columns.js';

export function executeQuery(records, request) {
  const normalizedRequest = normalizeQueryRequest(request);
  const refreshedRows = applyRefreshGeneration(records, normalizedRequest.refreshGeneration);
  const filteredRows = applyFilters(refreshedRows, normalizedRequest.filterGroup);
  const sortedRows = applySorting(filteredRows, normalizedRequest.sorts);
  const pageItems = sortedRows.slice(normalizedRequest.offset, normalizedRequest.offset + normalizedRequest.size);

  return {
    offset: normalizedRequest.offset,
    size: normalizedRequest.size,
    returnedCount: pageItems.length,
    totalCount: sortedRows.length,
    summary: calculateSummary(sortedRows, normalizedRequest.summaries),
    items: pageItems
  };
}

export function executeGroupedQuery(records, request) {
  const normalizedRequest = normalizeGroupedRequest(request);
  const effectiveSorts = buildEffectiveGroupSorts(normalizedRequest.sorts, normalizedRequest.groups);
  const refreshedRows = applyRefreshGeneration(records, normalizedRequest.refreshGeneration);
  const filteredRows = applyFilters(refreshedRows, normalizedRequest.filterGroup);
  const sortedRows = applySorting(filteredRows, effectiveSorts);

  if (normalizedRequest.groups.length === 0) {
    const rows = sortedRows
      .slice(normalizedRequest.offset, normalizedRequest.offset + normalizedRequest.size)
      .map((item) => ({
        kind: 'DataRow',
        level: 0,
        item
      }));

    return {
      offset: normalizedRequest.offset,
      size: normalizedRequest.size,
      returnedRowCount: rows.length,
      visibleRowCount: sortedRows.length,
      totalItemCount: sortedRows.length,
      topLevelGroupCount: 0,
      groupIds: [],
      summary: calculateSummary(sortedRows, normalizedRequest.summaries),
      rows
    };
  }

  const groupIds = [];
  const groupedTree = buildGroups(sortedRows, normalizedRequest.groups, 0, null, normalizedRequest.collapsedGroupIds, groupIds);
  const flattenedRows = [];
  flattenGroups(groupedTree, flattenedRows);
  const visibleWindow = flattenedRows.slice(normalizedRequest.offset, normalizedRequest.offset + normalizedRequest.size);

  return {
    offset: normalizedRequest.offset,
    size: normalizedRequest.size,
    returnedRowCount: visibleWindow.length,
    visibleRowCount: flattenedRows.length,
    totalItemCount: sortedRows.length,
    topLevelGroupCount: groupedTree.length,
    groupIds,
    summary: calculateSummary(sortedRows, normalizedRequest.summaries),
    rows: visibleWindow
  };
}

function normalizeQueryRequest(request = {}) {
  const size = positiveIntegerOrDefault(request.size, 50);
  return {
    offset: nonNegativeIntegerOrDefault(request.offset, 0),
    size,
    refreshGeneration: nonNegativeIntegerOrDefault(request.refreshGeneration, 0),
    sorts: Array.isArray(request.sorts) ? request.sorts : [],
    filterGroup: request.filterGroup ?? { logicalOperator: 'And', filters: [] },
    groups: Array.isArray(request.groups) ? request.groups : [],
    summaries: Array.isArray(request.summaries) ? request.summaries : []
  };
}

function normalizeGroupedRequest(request = {}) {
  const query = normalizeQueryRequest(request);
  return {
    ...query,
    collapsedGroupIds: new Set(Array.isArray(request.collapsedGroupIds) ? request.collapsedGroupIds.filter(Boolean) : [])
  };
}

function applyFilters(records, filterGroup) {
  const filters = Array.isArray(filterGroup?.filters) ? filterGroup.filters : [];
  if (filters.length === 0) {
    return records.slice();
  }

  const logicalOperator = String(filterGroup.logicalOperator || 'And').toLowerCase();
  return records.filter((record) => {
    const evaluations = filters.map((filter) => matchFilter(record, filter));
    return logicalOperator === 'or'
      ? evaluations.some(Boolean)
      : evaluations.every(Boolean);
  });
}

function applyRefreshGeneration(records, refreshGeneration) {
  if (!refreshGeneration || refreshGeneration <= 0) {
    return records.slice();
  }

  return records.map((record, index) => {
    const magnitude = refreshGeneration * (index % 3 + 1);
    const clone = { ...record };

    if (String(clone.GeometryType).toLowerCase() === 'polygon') {
      clone.AreaSquareMeters = Number(clone.AreaSquareMeters) + magnitude * 12.5;
    }

    if (String(clone.GeometryType).toLowerCase() === 'linestring') {
      clone.LengthMeters = Number(clone.LengthMeters) + magnitude * 8.75;
    }

    const inspectionDate = new Date(clone.LastInspection);
    inspectionDate.setUTCDate(inspectionDate.getUTCDate() + magnitude);
    clone.LastInspection = inspectionDate.toISOString();

    if (index % 4 === 0) {
      clone.Status = cycleStatus(clone.Status);
    }

    return clone;
  });
}

function matchFilter(record, filter) {
  const operator = String(filter?.operator || 'Equals').toLowerCase();
  const columnId = filter?.columnId;
  const value = normalizeValue(columnId, record[columnId]);
  const filterValue = normalizeValue(columnId, filter?.value);
  const secondValue = normalizeValue(columnId, filter?.secondValue);

  switch (operator) {
    case 'equals':
      return compareValues(value, filterValue) === 0;
    case 'contains':
      return String(value ?? '').toLowerCase().includes(String(filterValue ?? '').toLowerCase());
    case 'startswith':
      return String(value ?? '').toLowerCase().startsWith(String(filterValue ?? '').toLowerCase());
    case 'greaterthan':
      return compareValues(value, filterValue) > 0;
    case 'lessthan':
      return compareValues(value, filterValue) < 0;
    case 'between':
      return compareValues(value, filterValue) >= 0 && compareValues(value, secondValue) <= 0;
    case 'istrue':
      return value === true;
    case 'isfalse':
      return value === false;
    case 'custom':
      throw new Error('Custom filters are not supported over HTTP.');
    default:
      throw new Error(`Unsupported filter operator '${filter?.operator}'.`);
  }
}

function applySorting(records, sorts) {
  if (!Array.isArray(sorts) || sorts.length === 0) {
    return records.slice();
  }

  return records.slice().sort((left, right) => {
    for (const sort of sorts) {
      const direction = String(sort.direction || 'Ascending').toLowerCase() === 'descending' ? -1 : 1;
      const result = compareValues(
        normalizeValue(sort.columnId, left[sort.columnId]),
        normalizeValue(sort.columnId, right[sort.columnId])
      );

      if (result !== 0) {
        return result * direction;
      }
    }

    return 0;
  });
}

function calculateSummary(records, summaries) {
  const result = {};
  for (const summary of summaries) {
    const type = String(summary.type || 'Count');
    if (type.toLowerCase() === 'custom') {
      throw new Error('Custom summaries are not supported over HTTP.');
    }

    const values = records.map((record) => normalizeValue(summary.columnId, record[summary.columnId]));
    const key = `${summary.columnId}:${type}`;
    result[key] = calculateSummaryValue(values, type);
  }

  return result;
}

function calculateSummaryValue(values, type) {
  const numericValues = values.filter((value) => value !== null && value !== undefined).map(Number);

  switch (String(type).toLowerCase()) {
    case 'count':
      return values.length;
    case 'sum':
      return numericValues.reduce((total, value) => total + value, 0);
    case 'average':
      return numericValues.length === 0 ? 0 : numericValues.reduce((total, value) => total + value, 0) / numericValues.length;
    case 'min':
      return values.filter((value) => value !== null && value !== undefined).sort(compareValues)[0] ?? null;
    case 'max':
      return values.filter((value) => value !== null && value !== undefined).sort(compareValues).at(-1) ?? null;
    default:
      throw new Error(`Unsupported summary type '${type}'.`);
  }
}

function buildGroups(records, groups, level, parentId, collapsedGroupIds, groupIds) {
  if (level >= groups.length) {
    return [];
  }

  const groupDescriptor = groups[level];
  const buckets = new Map();
  for (const record of records) {
    const key = normalizeValue(groupDescriptor.columnId, record[groupDescriptor.columnId]);
    const stableKey = buildStableKey(key);
    if (!buckets.has(stableKey)) {
      buckets.set(stableKey, { key, items: [] });
    }

    buckets.get(stableKey).items.push(record);
  }

  const entries = [...buckets.values()].sort((left, right) => compareValues(left.key, right.key));
  if (String(groupDescriptor.direction || 'Ascending').toLowerCase() === 'descending') {
    entries.reverse();
  }

  return entries.map((entry) => {
    const groupId = buildGroupId(parentId, groupDescriptor.columnId, entry.key);
    groupIds.push(groupId);
    const children = buildGroups(entry.items, groups, level + 1, groupId, collapsedGroupIds, groupIds);
    const isExpanded = !collapsedGroupIds.has(groupId);

    return {
      kind: 'GroupHeader',
      level,
      groupId,
      groupColumnId: groupDescriptor.columnId,
      groupKey: entry.key,
      groupItemCount: entry.items.length,
      isExpanded,
      item: null,
      children,
      items: entry.items
    };
  });
}

function flattenGroups(groups, result) {
  for (const group of groups) {
    result.push({
      kind: group.kind,
      level: group.level,
      groupId: group.groupId,
      groupColumnId: group.groupColumnId,
      groupKey: group.groupKey,
      groupItemCount: group.groupItemCount,
      isExpanded: group.isExpanded,
      item: null
    });

    if (!group.isExpanded) {
      continue;
    }

    if (group.children.length > 0) {
      flattenGroups(group.children, result);
      continue;
    }

    for (const item of group.items) {
      result.push({
        kind: 'DataRow',
        level: group.level + 1,
        item
      });
    }
  }
}

function buildEffectiveGroupSorts(sorts, groups) {
  const effectiveSorts = groups.map((group) => ({
    columnId: group.columnId,
    direction: group.direction
  }));

  for (const sort of sorts) {
    if (!groups.some((group) => group.columnId === sort.columnId)) {
      effectiveSorts.push(sort);
    }
  }

  return effectiveSorts;
}

function normalizeValue(columnId, value) {
  if (value === null || value === undefined) {
    return null;
  }

  const column = gridColumnsById[columnId];
  if (!column) {
    return value;
  }

  switch (column.valueType) {
    case 'decimal':
      return typeof value === 'number' ? value : Number.parseFloat(String(value));
    case 'date':
      return typeof value === 'string' ? value : new Date(value).toISOString();
    case 'string':
      return String(value);
    default:
      return value;
  }
}

function compareValues(left, right) {
  if (left === right) {
    return 0;
  }

  if (left === null || left === undefined) {
    return -1;
  }

  if (right === null || right === undefined) {
    return 1;
  }

  if (typeof left === 'number' && typeof right === 'number') {
    return left - right;
  }

  return String(left).localeCompare(String(right), 'en', { sensitivity: 'base' });
}

function cycleStatus(currentStatus) {
  const statuses = ['Active', 'Verified', 'NeedsReview', 'UnderMaintenance', 'Planned', 'Retired'];
  const index = statuses.findIndex((status) => status.toLowerCase() === String(currentStatus ?? '').toLowerCase());
  return index < 0 ? statuses[0] : statuses[(index + 1) % statuses.length];
}

function buildGroupId(parentId, columnId, key) {
  const segment = `${columnId}:${buildStableKey(key)}`;
  return parentId ? `${parentId}/${segment}` : segment;
}

function buildStableKey(key) {
  return String(key ?? '');
}

function nonNegativeIntegerOrDefault(value, fallback) {
  const parsed = Number.parseInt(value ?? fallback, 10);
  return Number.isFinite(parsed) && parsed >= 0 ? parsed : fallback;
}

function positiveIntegerOrDefault(value, fallback) {
  const parsed = Number.parseInt(value ?? fallback, 10);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
}
