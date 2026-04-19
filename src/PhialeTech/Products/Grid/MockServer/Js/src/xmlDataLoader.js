import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const defaultXmlPath = path.resolve(
  __dirname,
  '..',
  '..',
  '..',
  '..',
  '..',
  '..',
  '..',
  'demo',
  'PhialeTech',
  'Shared',
  'PhialeTech.Components.Shared',
  'Data',
  'gis-grid-demo-530-regenerated.xml'
);

const fieldMap = {
  ObjectId: 'ObjectId',
  Category: 'Category',
  ObjectName: 'ObjectName',
  GeometryType: 'GeometryType',
  CRS: 'Crs',
  Municipality: 'Municipality',
  District: 'District',
  Status: 'Status',
  Area_m2: 'AreaSquareMeters',
  Length_m: 'LengthMeters',
  LastInspection: 'LastInspection',
  Source: 'Source',
  Priority: 'Priority',
  Visible: 'Visible',
  Editable: 'EditableFlag',
  Owner: 'Owner',
  ScaleHint: 'ScaleHint',
  Tags: 'Tags'
};

export function loadDefaultRecords() {
  return loadRecordsFromXml(defaultXmlPath);
}

export function loadRecordsFromXml(xmlPath) {
  const xml = fs.readFileSync(xmlPath, 'utf8');
  const recordMatches = [...xml.matchAll(/<Record>(.*?)<\/Record>/gs)];

  return recordMatches.map((match) => mapRecord(match[1]));
}

function mapRecord(recordXml) {
  const source = {};
  for (const [xmlName, propertyName] of Object.entries(fieldMap)) {
    source[propertyName] = readTag(recordXml, xmlName);
  }

  return {
    Id: source.ObjectId || cryptoRandomId(),
    Category: source.Category,
    ObjectId: source.ObjectId,
    ObjectName: source.ObjectName,
    GeometryType: source.GeometryType,
    Crs: source.Crs,
    Municipality: source.Municipality,
    District: source.District,
    Status: source.Status,
    AreaSquareMeters: parseDecimal(source.AreaSquareMeters),
    LengthMeters: parseDecimal(source.LengthMeters),
    LastInspection: parseDate(source.LastInspection),
    Source: source.Source,
    Priority: source.Priority,
    Visible: parseBoolean(source.Visible),
    EditableFlag: parseBoolean(source.EditableFlag),
    Owner: source.Owner,
    ScaleHint: parseInteger(source.ScaleHint),
    Tags: source.Tags
  };
}

function readTag(xml, tagName) {
  const match = xml.match(new RegExp(`<${tagName}>(.*?)<\\/${tagName}>`, 's'));
  return match ? decodeXml(match[1].trim()) : '';
}

function decodeXml(value) {
  return value
    .replaceAll('&lt;', '<')
    .replaceAll('&gt;', '>')
    .replaceAll('&apos;', "'")
    .replaceAll('&quot;', '"')
    .replaceAll('&amp;', '&');
}

function parseDecimal(value) {
  const parsed = Number.parseFloat(value);
  return Number.isFinite(parsed) ? parsed : 0;
}

function parseInteger(value) {
  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) ? parsed : 0;
}

function parseBoolean(value) {
  return String(value).toLowerCase() === 'true';
}

function parseDate(value) {
  const parsed = new Date(`${value}T00:00:00.000Z`);
  return Number.isNaN(parsed.getTime()) ? null : parsed.toISOString();
}

function cryptoRandomId() {
  return `auto-${Math.random().toString(16).slice(2)}`;
}
