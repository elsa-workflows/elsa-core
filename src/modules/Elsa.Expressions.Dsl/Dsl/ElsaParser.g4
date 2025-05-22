parser grammar ElsaParser;

options { tokenVocab = ElsaLexer; }

program                
    :   (stat | LINE_COMMENT)*
    ;

object
    :   ID objectInitializer?
    ;
    
newObject
    :   NEW ID ('<' type '>')? '(' args? ')'
    ;

varDecl             
    :   VARIABLE ID (':' type)? (EQ expr)?
    ;
    
localVarDecl
    :   'let' ID (':' type)? (EQ expr)?
    ;

type
    :   VOID
    |   FLOAT
    |   INT
    |   OBJECT
    |   STRING
    |   ID
    ;
    
expressionMarker
    :   EXPRESSION_MARKER '(' ID ',' expressionContent ')'
    |   ID '=>' expressionContent
    ;
    
expressionContent
    :   .*?
    ;
     
methodCall
    :   ID '.' funcCall
    ;
    
funcCall
    :   ID '(' args? ')'
    ;

args
    :   arg (',' arg)*
    ;
    
arg
    :   expr
    |   expressionMarker
    ;
    
block
    :   '{' stat* '}'
    ;
    
objectInitializer
    :   '{' propertyList? '}'
    ;
    
propertyList
    :   property (',' property)*
    ;
    
property
    :   ID ':' expr
    ;
    
stat
    :   object ';'                                              #objectStat       
    |   'if' expr 'then' thenStat ('else' elseStat)?            #ifStat
    |   'for' '(' ID '=' expr ';' expr ';' expr ')' stat        #forStat
    |   'return' expr? ';'                                      #returnStat                              
    |   block                                                   #blockStat
    |   varDecl ';'                                             #variableDeclarationStat
    |   localVarDecl ';'                                        #localVariableDeclarationStat
    |   expr '=' expr ';'                                       #assignmentStat
    |   expr ';'                                                #expressionStat
    ;

thenStat 
    :   stat
    ;
    
elseStat 
    :   stat
    ;

expr
    :   funcCall                                                #functionExpr
    |   expressionMarker                                        #expressionMarkerExpr
    |   object                                                  #objectExpr
    |   newObject                                               #newObjectExpr
    |   expr '++'                                               #incrementExpr
    |   expr '--'                                               #decrementExpr
    |   '-' expr                                                #negateExpr
    |   '!' expr                                                #notExpr
    |   expr '*' expr                                           #multiplyExpr
    |   expr '+' expr                                           #addExpr
    |   expr '-' expr                                           #subtractExpr
    |   expr ('==' | '>' | '<' | '!=' | '>=' | '<=') expr       #compareExpr
    |   INTEGER_VAL                                             #integerValueExpr
    |   STRING_VAL                                              #stringValueExpr
    |   BACKTICKSTRING_VAL                                      #backTickStringValueExpr
    |   '(' exprList? ')'                                       #parenthesesExpr
    |   '[' exprList? ']'                                       #bracketsExpr
    |   methodCall                                              #methodCallExpr
    |   ID                                                      #variableExpr
    ;
    
exprList
    :   expr (',' expr)*
    ;
   