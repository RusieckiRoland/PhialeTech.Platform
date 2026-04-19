import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import {
  REPORT_SECTION_KEYS,
  SPECIAL_FIELD_KINDS,
  clampColumnCount,
  formatFieldListText,
  formatTableColumnsText,
  getSectionKeyFromTargetId,
  getSectionTargetId,
  isSectionTargetId,
  isSelectionTargetSelected,
  parseFieldListText,
  parseTableColumnsText,
  resolveActionTarget,
  resolveSpecialFieldValue,
  shouldRenderPageSection,
} from '../../../../../src/PhialeTech/Shared/PhialeTech.WebAssets/Assets/ReportDesigner/report-designer-definition-helpers.js';

test('parseTableColumnsText keeps width and alignment metadata', () => {
  const parsed = parseTableColumnsText('Netto|Net|currency||PLN|2|120px|right');

  assert.equal(parsed.length, 1);
  assert.equal(parsed[0].header, 'Netto');
  assert.equal(parsed[0].width, '120px');
  assert.equal(parsed[0].textAlign, 'right');
  assert.equal(parsed[0].format.kind, 'currency');
});

test('formatTableColumnsText round-trips column metadata into editor text', () => {
  const text = formatTableColumnsText([{
    header: 'Amount',
    binding: 'Total',
    width: '25%',
    textAlign: 'right',
    format: { kind: 'currency', pattern: '', currency: 'PLN', decimals: 2 },
  }]);

  assert.equal(text, 'Amount|Total|currency||PLN|2|25%|right');
});

test('field list helpers parse and format label-value definitions', () => {
  const parsed = parseFieldListText('Numer faktury|InvoiceNumber|text|||');

  assert.equal(parsed[0].label, 'Numer faktury');
  assert.equal(parsed[0].binding, 'InvoiceNumber');
  assert.equal(formatFieldListText(parsed), 'Numer faktury|InvoiceNumber|text|||');
});

test('resolveSpecialFieldValue returns localized page summary and date text', () => {
  assert.equal(resolveSpecialFieldValue('PageNumberOfTotalPages', {
    locale: 'pl',
    currentPageNumber: 2,
    totalPages: 7,
  }), 'Strona 2 z 7');

  assert.equal(resolveSpecialFieldValue('CurrentDate', {
    locale: 'en',
    datePattern: 'yyyy-MM-dd',
    now: new Date('2026-03-28T10:15:00Z'),
  }), '2026-03-28');
});

test('clampColumnCount keeps columns in the supported desktop range', () => {
  assert.equal(clampColumnCount(1), 2);
  assert.equal(clampColumnCount(2), 2);
  assert.equal(clampColumnCount(3), 3);
  assert.equal(clampColumnCount(6), 3);
});

test('special field kind list includes the planned automatic page markers', () => {
  assert.deepEqual(SPECIAL_FIELD_KINDS, [
    'CurrentDate',
    'PageNumber',
    'TotalPages',
    'PageNumberOfTotalPages',
  ]);
});

test('isSelectionTargetSelected compares preview and inspector targets by stable id', () => {
  assert.equal(isSelectionTargetSelected('block-1', 'block-1'), true);
  assert.equal(isSelectionTargetSelected('block-1', 'block-2'), false);
  assert.equal(isSelectionTargetSelected('block-1', ''), false);
});

test('resolveActionTarget keeps normal click on top block and ctrl-click on underlying parent block', () => {
  const actions = [
    { action: 'select-block', blockId: 'field-list' },
    { action: 'select-block', blockId: 'header-left-column' },
    { action: 'select-page' },
  ];

  assert.deepEqual(resolveActionTarget(actions, false), actions[0]);
  assert.deepEqual(resolveActionTarget(actions, true), actions[1]);
});

test('resolveActionTarget does not override non-selection actions when ctrl is pressed', () => {
  const actions = [
    { action: 'move-block-up', blockId: 'field-list' },
    { action: 'select-block', blockId: 'field-list' },
    { action: 'select-block', blockId: 'header-left-column' },
  ];

  assert.deepEqual(resolveActionTarget(actions, true), actions[0]);
});

test('report section helpers expose stable ids and supported keys', () => {
  assert.deepEqual(REPORT_SECTION_KEYS, [
    'reportHeader',
    'body',
    'reportFooter',
    'pageHeader',
    'pageFooter',
  ]);

  const targetId = getSectionTargetId('pageFooter');
  assert.equal(isSectionTargetId(targetId), true);
  assert.equal(getSectionKeyFromTargetId(targetId), 'pageFooter');
  assert.equal(getSectionKeyFromTargetId('block-1'), '');
});

test('shouldRenderPageSection respects skip first and skip last rules', () => {
  assert.equal(shouldRenderPageSection(1, 3, true, false), false);
  assert.equal(shouldRenderPageSection(2, 3, true, false), true);
  assert.equal(shouldRenderPageSection(3, 3, false, true), false);
  assert.equal(shouldRenderPageSection(2, 3, false, true), true);
});

test('shell field selector includes section fields so page-band checkboxes persist changes', async () => {
  const hostScript = await readFile(
    new URL('../../../../../src/PhialeTech/Shared/PhialeTech.WebAssets/Assets/ReportDesigner/phiale-report-designer-host.js', import.meta.url),
    'utf8');

  assert.match(hostScript, /SHELL_FIELD_SELECTOR\s*=\s*"[^"]*\[data-section-field\][^"]*"/);
});
