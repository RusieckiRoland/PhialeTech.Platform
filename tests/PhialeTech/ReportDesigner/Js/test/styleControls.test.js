import test from 'node:test';
import assert from 'node:assert/strict';
import {
  BASIC_COLOR_SWATCHES,
  composeBorderValue,
  composeBoxSpacingValue,
  composeLengthValue,
  normalizeHexColor,
  parseBorderValue,
  parseBoxSpacingValue,
  parseLengthValue,
  toColorInputValue,
} from '../../../../../src/PhialeTech/Shared/PhialeTech.WebAssets/Assets/ReportDesigner/report-designer-style-controls.js';

test('parseLengthValue and composeLengthValue round-trip css length values', () => {
  const parsed = parseLengthValue('22px');

  assert.equal(parsed.kind, 'length');
  assert.equal(parsed.amount, '22');
  assert.equal(parsed.unit, 'px');
  assert.equal(composeLengthValue(parsed), '22px');
});

test('parseBoxSpacingValue expands shorthand and composeBoxSpacingValue keeps four-side output', () => {
  const parsed = parseBoxSpacingValue('0 0 18px 0');

  assert.equal(parsed.kind, 'box');
  assert.deepEqual(
    { top: parsed.top, right: parsed.right, bottom: parsed.bottom, left: parsed.left, unit: parsed.unit },
    { top: '0', right: '0', bottom: '18', left: '0', unit: 'px' });
  assert.equal(composeBoxSpacingValue(parsed), '0 0 18px 0');
});

test('parseBorderValue extracts width style and rgba color', () => {
  const parsed = parseBorderValue('1px solid rgba(203, 213, 225, 0.82)');

  assert.equal(parsed.kind, 'border');
  assert.equal(parsed.enabled, true);
  assert.equal(parsed.width, '1');
  assert.equal(parsed.unit, 'px');
  assert.equal(parsed.style, 'solid');
  assert.equal(parsed.color, 'rgba(203, 213, 225, 0.82)');
});

test('composeBorderValue returns none when border is disabled', () => {
  const composed = composeBorderValue({
    kind: 'border',
    enabled: false,
    width: '2',
    unit: 'px',
    style: 'dashed',
    color: '#0f766e',
  });

  assert.equal(composed, 'none');
});

test('normalizeHexColor expands short hex colors and rejects non-hex input', () => {
  assert.equal(normalizeHexColor('#AbC'), '#aabbcc');
  assert.equal(normalizeHexColor('#0f172a'), '#0f172a');
  assert.equal(normalizeHexColor('rgb(15, 23, 42)'), '');
});

test('toColorInputValue falls back to a safe hex value for the native color picker', () => {
  assert.equal(toColorInputValue('#0f766e'), '#0f766e');
  assert.equal(toColorInputValue('rgba(15, 118, 110, 0.8)', '#ffffff'), '#ffffff');
  assert.equal(toColorInputValue('', ''), '#0f172a');
});

test('basic color swatches contain the default dark slate and white endpoints', () => {
  assert.ok(BASIC_COLOR_SWATCHES.includes('#0f172a'));
  assert.ok(BASIC_COLOR_SWATCHES.includes('#ffffff'));
});
