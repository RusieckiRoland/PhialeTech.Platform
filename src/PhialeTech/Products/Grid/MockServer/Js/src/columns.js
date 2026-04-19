export const gridColumns = [
  { id: 'Category', header: 'Category', valueType: 'string', width: 150, isEditable: false, isVisible: true },
  { id: 'ObjectName', header: 'Object name', valueType: 'string', width: 260, isEditable: true, isVisible: true },
  { id: 'ObjectId', header: 'Object ID', valueType: 'string', width: 180, isEditable: false, isVisible: true },
  { id: 'GeometryType', header: 'Geometry type', valueType: 'string', width: 130, isEditable: false, isVisible: true },
  { id: 'Municipality', header: 'Municipality', valueType: 'string', width: 130, isEditable: false, isVisible: true },
  { id: 'District', header: 'District', valueType: 'string', width: 140, isEditable: false, isVisible: true },
  { id: 'Status', header: 'Status', valueType: 'string', width: 150, isEditable: true, isVisible: true },
  { id: 'Priority', header: 'Priority', valueType: 'string', width: 120, isEditable: true, isVisible: true },
  { id: 'AreaSquareMeters', header: 'Area [m2]', valueType: 'decimal', width: 140, isEditable: false, isVisible: true },
  { id: 'LengthMeters', header: 'Length [m]', valueType: 'decimal', width: 140, isEditable: false, isVisible: true },
  { id: 'LastInspection', header: 'Last inspection', valueType: 'date', width: 150, isEditable: false, isVisible: true },
  { id: 'Owner', header: 'Owner', valueType: 'string', width: 180, isEditable: true, isVisible: true }
];

export const gridColumnsById = Object.freeze(
  Object.fromEntries(gridColumns.map((column) => [column.id, column]))
);
