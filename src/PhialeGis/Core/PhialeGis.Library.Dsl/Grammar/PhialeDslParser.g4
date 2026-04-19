parser grammar PhialeDslParser;
options { tokenVocab=PhialeDslLexer; }

script : (command SEMI?)* EOF ;

command
    : zoomIn
    | zoomOut
    | zoom
    | addLineStart          // ⬅️ NOWE: "ADD LINESTRING" (bez współrzędnych)
    ;

zoomIn  : KW_ZOOMIN ;
zoomOut : KW_ZOOMOUT ;
zoom    : KW_ZOOM NUMBER ;

// ⬅️ NOWE: minimalne wejście w tryb podawania punktów (PIM) dla linii
addLineStart : KW_ADD KW_LINESTRING ;

// UŻYWANE TYLKO W TRYBIE PUNKTÓW (lexer: mode POINTS)
pointsLine
    : ptAbs
    | ptRel
    | ptPolar
    | ptUndo
    | emptyLine          // pusty Enter (EOF)
    ;

ptAbs     : PT_ABS   ;
ptRel     : PT_REL   ;
ptPolar   : PT_POLAR ;
ptUndo    : PT_UNDO  ;
emptyLine : EOF      ;
