lexer grammar PhialeDslLexer;
options { caseInsensitive = true; }  // matching tokenów bez względu na case

/* ===== Keywords ===== */
KW_ZOOMIN     : 'ZoomIn';
KW_ZOOMOUT    : 'ZoomOut';
KW_ZOOM       : 'Zoom';
KW_ADD        : 'Add';         // ⬅️ NOWE
KW_LINESTRING : 'LineString';  // ⬅️ NOWE

/* ===== Literals ===== */
NUMBER : DIGITS ('.' DIGITS)? ;
fragment DIGITS : [0-9]+ ;

/* ===== Symbols ===== */
SEMI : ';' ;

/* ===== Whitespace & Comments ===== */
WS : [ \t\r\n]+ -> skip ;
COMMENT : '//' ~[\r\n]* -> skip ;

// --- TRYB PUNKTÓW: używany tylko podczas trwania akcji (jedna linia wejścia) ---
mode POINTS;

// UNDO / U
PT_UNDO  : 'UNDO' | 'U' ;

// x y  (absolutne)
fragment INT_  : [0-9]+ ;
fragment NUM_  : INT_ ('.' INT_)? ;
fragment SP_   : [ \t]+ ;

PT_ABS   : NUM_ (SP_)? NUM_ ;

// @dx dy  (względne)
PT_REL   : '@' (SP_)? NUM_ SP_+ NUM_ ;

// <angle dist>  (biegunowo; kąt w stopniach)
PT_POLAR : '<' (SP_)? NUM_ SP_+ NUM_ (SP_)? '>' ;

// białe znaki w trybie punktów
PT_WS : [ \t]+ -> skip ;
