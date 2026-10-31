// Generated from C:/Projects/Elsa/v3/src/dsl/Elsa.Dsl/Dsl\ElsaParser.g4 by ANTLR 4.9.2
import org.antlr.v4.runtime.atn.*;
import org.antlr.v4.runtime.dfa.DFA;
import org.antlr.v4.runtime.*;
import org.antlr.v4.runtime.misc.*;
import org.antlr.v4.runtime.tree.*;
import java.util.List;
import java.util.Iterator;
import java.util.ArrayList;

@SuppressWarnings({"all", "warnings", "unchecked", "unused", "cast"})
public class ElsaParser extends Parser {
	static { RuntimeMetaData.checkVersion("4.9.2", RuntimeMetaData.VERSION); }

	protected static final DFA[] _decisionToDFA;
	protected static final PredictionContextCache _sharedContextCache =
		new PredictionContextCache();
	public static final int
		EQ=1, GREATER=2, INCREMENT=3, DECREMENT=4, NEW=5, VARIABLE=6, LET=7, IF=8, 
		THEN=9, ELSE=10, FOR=11, RETURN=12, VOID=13, FLOAT=14, INT=15, STRING=16, 
		OBJECT=17, EXPRESSION_MARKER=18, SYMBOL=19, COLON=20, SEMICOLON=21, COMMA=22, 
		PLUS=23, MINUS=24, STAR=25, EQUALS=26, NOT_EQUALS=27, GREATER_EQUALS=28, 
		LESS=29, LESS_EQUALS=30, LAMBDA=31, PARENTHESES_OPEN=32, PARENTHESES_CLOSE=33, 
		BRACKET_OPEN=34, BRACKET_CLOSE=35, CURLYBRACE_OPEN=36, CURLYBRACE_CLOSE=37, 
		EXCLAMATION=38, PIPE=39, PIPE_DBL=40, PERIOD=41, STRING_VAL=42, BACKTICKSTRING_VAL=43, 
		LINE_COMMENT=44, INTEGER_VAL=45, ID=46, WS=47, ESC=48;
	public static final int
		RULE_program = 0, RULE_object = 1, RULE_newObject = 2, RULE_varDecl = 3, 
		RULE_localVarDecl = 4, RULE_type = 5, RULE_expressionMarker = 6, RULE_expressionContent = 7, 
		RULE_methodCall = 8, RULE_funcCall = 9, RULE_args = 10, RULE_arg = 11, 
		RULE_block = 12, RULE_objectInitializer = 13, RULE_propertyList = 14, 
		RULE_property = 15, RULE_stat = 16, RULE_thenStat = 17, RULE_elseStat = 18, 
		RULE_expr = 19, RULE_exprList = 20;
	private static String[] makeRuleNames() {
		return new String[] {
			"program", "object", "newObject", "varDecl", "localVarDecl", "type", 
			"expressionMarker", "expressionContent", "methodCall", "funcCall", "args", 
			"arg", "block", "objectInitializer", "propertyList", "property", "stat", 
			"thenStat", "elseStat", "expr", "exprList"
		};
	}
	public static final String[] ruleNames = makeRuleNames();

	private static String[] makeLiteralNames() {
		return new String[] {
			null, "'='", "'>'", "'++'", "'--'", "'new'", "'variable'", "'let'", "'if'", 
			"'then'", "'else'", "'for'", "'return'", "'void'", "'float'", "'int'", 
			"'string'", "'object'", "'expression'", null, "':'", "';'", "','", "'+'", 
			"'-'", "'*'", "'=='", "'!='", "'>='", "'<'", "'<='", "'=>'", "'('", "')'", 
			"'['", "']'", "'{'", "'}'", "'!'", "'|'", "'||'", "'.'", null, null, 
			null, null, null, null, "'\\|'"
		};
	}
	private static final String[] _LITERAL_NAMES = makeLiteralNames();
	private static String[] makeSymbolicNames() {
		return new String[] {
			null, "EQ", "GREATER", "INCREMENT", "DECREMENT", "NEW", "VARIABLE", "LET", 
			"IF", "THEN", "ELSE", "FOR", "RETURN", "VOID", "FLOAT", "INT", "STRING", 
			"OBJECT", "EXPRESSION_MARKER", "SYMBOL", "COLON", "SEMICOLON", "COMMA", 
			"PLUS", "MINUS", "STAR", "EQUALS", "NOT_EQUALS", "GREATER_EQUALS", "LESS", 
			"LESS_EQUALS", "LAMBDA", "PARENTHESES_OPEN", "PARENTHESES_CLOSE", "BRACKET_OPEN", 
			"BRACKET_CLOSE", "CURLYBRACE_OPEN", "CURLYBRACE_CLOSE", "EXCLAMATION", 
			"PIPE", "PIPE_DBL", "PERIOD", "STRING_VAL", "BACKTICKSTRING_VAL", "LINE_COMMENT", 
			"INTEGER_VAL", "ID", "WS", "ESC"
		};
	}
	private static final String[] _SYMBOLIC_NAMES = makeSymbolicNames();
	public static final Vocabulary VOCABULARY = new VocabularyImpl(_LITERAL_NAMES, _SYMBOLIC_NAMES);

	/**
	 * @deprecated Use {@link #VOCABULARY} instead.
	 */
	@Deprecated
	public static final String[] tokenNames;
	static {
		tokenNames = new String[_SYMBOLIC_NAMES.length];
		for (int i = 0; i < tokenNames.length; i++) {
			tokenNames[i] = VOCABULARY.getLiteralName(i);
			if (tokenNames[i] == null) {
				tokenNames[i] = VOCABULARY.getSymbolicName(i);
			}

			if (tokenNames[i] == null) {
				tokenNames[i] = "<INVALID>";
			}
		}
	}

	@Override
	@Deprecated
	public String[] getTokenNames() {
		return tokenNames;
	}

	@Override

	public Vocabulary getVocabulary() {
		return VOCABULARY;
	}

	@Override
	public String getGrammarFileName() { return "ElsaParser.g4"; }

	@Override
	public String[] getRuleNames() { return ruleNames; }

	@Override
	public String getSerializedATN() { return _serializedATN; }

	@Override
	public ATN getATN() { return _ATN; }

	public ElsaParser(TokenStream input) {
		super(input);
		_interp = new ParserATNSimulator(this,_ATN,_decisionToDFA,_sharedContextCache);
	}

	public static class ProgramContext extends ParserRuleContext {
		public List<StatContext> stat() {
			return getRuleContexts(StatContext.class);
		}
		public StatContext stat(int i) {
			return getRuleContext(StatContext.class,i);
		}
		public List<TerminalNode> LINE_COMMENT() { return getTokens(ElsaParser.LINE_COMMENT); }
		public TerminalNode LINE_COMMENT(int i) {
			return getToken(ElsaParser.LINE_COMMENT, i);
		}
		public ProgramContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_program; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterProgram(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitProgram(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitProgram(this);
			else return visitor.visitChildren(this);
		}
	}

	public final ProgramContext program() throws RecognitionException {
		ProgramContext _localctx = new ProgramContext(_ctx, getState());
		enterRule(_localctx, 0, RULE_program);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(46);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while ((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << NEW) | (1L << VARIABLE) | (1L << LET) | (1L << IF) | (1L << FOR) | (1L << RETURN) | (1L << MINUS) | (1L << PARENTHESES_OPEN) | (1L << BRACKET_OPEN) | (1L << CURLYBRACE_OPEN) | (1L << EXCLAMATION) | (1L << STRING_VAL) | (1L << BACKTICKSTRING_VAL) | (1L << LINE_COMMENT) | (1L << INTEGER_VAL) | (1L << ID))) != 0)) {
				{
				setState(44);
				_errHandler.sync(this);
				switch (_input.LA(1)) {
				case NEW:
				case VARIABLE:
				case LET:
				case IF:
				case FOR:
				case RETURN:
				case MINUS:
				case PARENTHESES_OPEN:
				case BRACKET_OPEN:
				case CURLYBRACE_OPEN:
				case EXCLAMATION:
				case STRING_VAL:
				case BACKTICKSTRING_VAL:
				case INTEGER_VAL:
				case ID:
					{
					setState(42);
					stat();
					}
					break;
				case LINE_COMMENT:
					{
					setState(43);
					match(LINE_COMMENT);
					}
					break;
				default:
					throw new NoViableAltException(this);
				}
				}
				setState(48);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class ObjectContext extends ParserRuleContext {
		public TerminalNode ID() { return getToken(ElsaParser.ID, 0); }
		public ObjectInitializerContext objectInitializer() {
			return getRuleContext(ObjectInitializerContext.class,0);
		}
		public ObjectContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_object; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterObject(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitObject(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitObject(this);
			else return visitor.visitChildren(this);
		}
	}

	public final ObjectContext object() throws RecognitionException {
		ObjectContext _localctx = new ObjectContext(_ctx, getState());
		enterRule(_localctx, 2, RULE_object);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(49);
			match(ID);
			setState(51);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,2,_ctx) ) {
			case 1:
				{
				setState(50);
				objectInitializer();
				}
				break;
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class NewObjectContext extends ParserRuleContext {
		public TerminalNode NEW() { return getToken(ElsaParser.NEW, 0); }
		public TerminalNode ID() { return getToken(ElsaParser.ID, 0); }
		public TerminalNode PARENTHESES_OPEN() { return getToken(ElsaParser.PARENTHESES_OPEN, 0); }
		public TerminalNode PARENTHESES_CLOSE() { return getToken(ElsaParser.PARENTHESES_CLOSE, 0); }
		public TerminalNode LESS() { return getToken(ElsaParser.LESS, 0); }
		public TypeContext type() {
			return getRuleContext(TypeContext.class,0);
		}
		public TerminalNode GREATER() { return getToken(ElsaParser.GREATER, 0); }
		public ArgsContext args() {
			return getRuleContext(ArgsContext.class,0);
		}
		public NewObjectContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_newObject; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterNewObject(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitNewObject(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitNewObject(this);
			else return visitor.visitChildren(this);
		}
	}

	public final NewObjectContext newObject() throws RecognitionException {
		NewObjectContext _localctx = new NewObjectContext(_ctx, getState());
		enterRule(_localctx, 4, RULE_newObject);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(53);
			match(NEW);
			setState(54);
			match(ID);
			setState(59);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==LESS) {
				{
				setState(55);
				match(LESS);
				setState(56);
				type();
				setState(57);
				match(GREATER);
				}
			}

			setState(61);
			match(PARENTHESES_OPEN);
			setState(63);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if ((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << NEW) | (1L << EXPRESSION_MARKER) | (1L << MINUS) | (1L << PARENTHESES_OPEN) | (1L << BRACKET_OPEN) | (1L << EXCLAMATION) | (1L << STRING_VAL) | (1L << BACKTICKSTRING_VAL) | (1L << INTEGER_VAL) | (1L << ID))) != 0)) {
				{
				setState(62);
				args();
				}
			}

			setState(65);
			match(PARENTHESES_CLOSE);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class VarDeclContext extends ParserRuleContext {
		public TerminalNode VARIABLE() { return getToken(ElsaParser.VARIABLE, 0); }
		public TerminalNode ID() { return getToken(ElsaParser.ID, 0); }
		public TerminalNode COLON() { return getToken(ElsaParser.COLON, 0); }
		public TypeContext type() {
			return getRuleContext(TypeContext.class,0);
		}
		public TerminalNode EQ() { return getToken(ElsaParser.EQ, 0); }
		public ExprContext expr() {
			return getRuleContext(ExprContext.class,0);
		}
		public VarDeclContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_varDecl; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterVarDecl(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitVarDecl(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitVarDecl(this);
			else return visitor.visitChildren(this);
		}
	}

	public final VarDeclContext varDecl() throws RecognitionException {
		VarDeclContext _localctx = new VarDeclContext(_ctx, getState());
		enterRule(_localctx, 6, RULE_varDecl);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(67);
			match(VARIABLE);
			setState(68);
			match(ID);
			setState(71);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==COLON) {
				{
				setState(69);
				match(COLON);
				setState(70);
				type();
				}
			}

			setState(75);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==EQ) {
				{
				setState(73);
				match(EQ);
				setState(74);
				expr(0);
				}
			}

			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class LocalVarDeclContext extends ParserRuleContext {
		public TerminalNode LET() { return getToken(ElsaParser.LET, 0); }
		public TerminalNode ID() { return getToken(ElsaParser.ID, 0); }
		public TerminalNode COLON() { return getToken(ElsaParser.COLON, 0); }
		public TypeContext type() {
			return getRuleContext(TypeContext.class,0);
		}
		public TerminalNode EQ() { return getToken(ElsaParser.EQ, 0); }
		public ExprContext expr() {
			return getRuleContext(ExprContext.class,0);
		}
		public LocalVarDeclContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_localVarDecl; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterLocalVarDecl(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitLocalVarDecl(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitLocalVarDecl(this);
			else return visitor.visitChildren(this);
		}
	}

	public final LocalVarDeclContext localVarDecl() throws RecognitionException {
		LocalVarDeclContext _localctx = new LocalVarDeclContext(_ctx, getState());
		enterRule(_localctx, 8, RULE_localVarDecl);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(77);
			match(LET);
			setState(78);
			match(ID);
			setState(81);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==COLON) {
				{
				setState(79);
				match(COLON);
				setState(80);
				type();
				}
			}

			setState(85);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==EQ) {
				{
				setState(83);
				match(EQ);
				setState(84);
				expr(0);
				}
			}

			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class TypeContext extends ParserRuleContext {
		public TerminalNode VOID() { return getToken(ElsaParser.VOID, 0); }
		public TerminalNode FLOAT() { return getToken(ElsaParser.FLOAT, 0); }
		public TerminalNode INT() { return getToken(ElsaParser.INT, 0); }
		public TerminalNode OBJECT() { return getToken(ElsaParser.OBJECT, 0); }
		public TerminalNode STRING() { return getToken(ElsaParser.STRING, 0); }
		public TerminalNode ID() { return getToken(ElsaParser.ID, 0); }
		public TypeContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_type; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterType(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitType(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitType(this);
			else return visitor.visitChildren(this);
		}
	}

	public final TypeContext type() throws RecognitionException {
		TypeContext _localctx = new TypeContext(_ctx, getState());
		enterRule(_localctx, 10, RULE_type);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(87);
			_la = _input.LA(1);
			if ( !((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << VOID) | (1L << FLOAT) | (1L << INT) | (1L << STRING) | (1L << OBJECT) | (1L << ID))) != 0)) ) {
			_errHandler.recoverInline(this);
			}
			else {
				if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
				_errHandler.reportMatch(this);
				consume();
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class ExpressionMarkerContext extends ParserRuleContext {
		public TerminalNode EXPRESSION_MARKER() { return getToken(ElsaParser.EXPRESSION_MARKER, 0); }
		public TerminalNode PARENTHESES_OPEN() { return getToken(ElsaParser.PARENTHESES_OPEN, 0); }
		public TerminalNode ID() { return getToken(ElsaParser.ID, 0); }
		public TerminalNode COMMA() { return getToken(ElsaParser.COMMA, 0); }
		public ExpressionContentContext expressionContent() {
			return getRuleContext(ExpressionContentContext.class,0);
		}
		public TerminalNode PARENTHESES_CLOSE() { return getToken(ElsaParser.PARENTHESES_CLOSE, 0); }
		public TerminalNode LAMBDA() { return getToken(ElsaParser.LAMBDA, 0); }
		public ExpressionMarkerContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_expressionMarker; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterExpressionMarker(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitExpressionMarker(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitExpressionMarker(this);
			else return visitor.visitChildren(this);
		}
	}

	public final ExpressionMarkerContext expressionMarker() throws RecognitionException {
		ExpressionMarkerContext _localctx = new ExpressionMarkerContext(_ctx, getState());
		enterRule(_localctx, 12, RULE_expressionMarker);
		try {
			setState(99);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case EXPRESSION_MARKER:
				enterOuterAlt(_localctx, 1);
				{
				setState(89);
				match(EXPRESSION_MARKER);
				setState(90);
				match(PARENTHESES_OPEN);
				setState(91);
				match(ID);
				setState(92);
				match(COMMA);
				setState(93);
				expressionContent();
				setState(94);
				match(PARENTHESES_CLOSE);
				}
				break;
			case ID:
				enterOuterAlt(_localctx, 2);
				{
				setState(96);
				match(ID);
				setState(97);
				match(LAMBDA);
				setState(98);
				expressionContent();
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class ExpressionContentContext extends ParserRuleContext {
		public ExpressionContentContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_expressionContent; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterExpressionContent(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitExpressionContent(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitExpressionContent(this);
			else return visitor.visitChildren(this);
		}
	}

	public final ExpressionContentContext expressionContent() throws RecognitionException {
		ExpressionContentContext _localctx = new ExpressionContentContext(_ctx, getState());
		enterRule(_localctx, 14, RULE_expressionContent);
		try {
			int _alt;
			enterOuterAlt(_localctx, 1);
			{
			setState(104);
			_errHandler.sync(this);
			_alt = getInterpreter().adaptivePredict(_input,10,_ctx);
			while ( _alt!=1 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1+1 ) {
					{
					{
					setState(101);
					matchWildcard();
					}
					} 
				}
				setState(106);
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,10,_ctx);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class MethodCallContext extends ParserRuleContext {
		public TerminalNode ID() { return getToken(ElsaParser.ID, 0); }
		public TerminalNode PERIOD() { return getToken(ElsaParser.PERIOD, 0); }
		public FuncCallContext funcCall() {
			return getRuleContext(FuncCallContext.class,0);
		}
		public MethodCallContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_methodCall; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterMethodCall(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitMethodCall(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitMethodCall(this);
			else return visitor.visitChildren(this);
		}
	}

	public final MethodCallContext methodCall() throws RecognitionException {
		MethodCallContext _localctx = new MethodCallContext(_ctx, getState());
		enterRule(_localctx, 16, RULE_methodCall);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(107);
			match(ID);
			setState(108);
			match(PERIOD);
			setState(109);
			funcCall();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class FuncCallContext extends ParserRuleContext {
		public TerminalNode ID() { return getToken(ElsaParser.ID, 0); }
		public TerminalNode PARENTHESES_OPEN() { return getToken(ElsaParser.PARENTHESES_OPEN, 0); }
		public TerminalNode PARENTHESES_CLOSE() { return getToken(ElsaParser.PARENTHESES_CLOSE, 0); }
		public ArgsContext args() {
			return getRuleContext(ArgsContext.class,0);
		}
		public FuncCallContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_funcCall; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterFuncCall(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitFuncCall(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitFuncCall(this);
			else return visitor.visitChildren(this);
		}
	}

	public final FuncCallContext funcCall() throws RecognitionException {
		FuncCallContext _localctx = new FuncCallContext(_ctx, getState());
		enterRule(_localctx, 18, RULE_funcCall);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(111);
			match(ID);
			setState(112);
			match(PARENTHESES_OPEN);
			setState(114);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if ((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << NEW) | (1L << EXPRESSION_MARKER) | (1L << MINUS) | (1L << PARENTHESES_OPEN) | (1L << BRACKET_OPEN) | (1L << EXCLAMATION) | (1L << STRING_VAL) | (1L << BACKTICKSTRING_VAL) | (1L << INTEGER_VAL) | (1L << ID))) != 0)) {
				{
				setState(113);
				args();
				}
			}

			setState(116);
			match(PARENTHESES_CLOSE);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class ArgsContext extends ParserRuleContext {
		public List<ArgContext> arg() {
			return getRuleContexts(ArgContext.class);
		}
		public ArgContext arg(int i) {
			return getRuleContext(ArgContext.class,i);
		}
		public List<TerminalNode> COMMA() { return getTokens(ElsaParser.COMMA); }
		public TerminalNode COMMA(int i) {
			return getToken(ElsaParser.COMMA, i);
		}
		public ArgsContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_args; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterArgs(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitArgs(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitArgs(this);
			else return visitor.visitChildren(this);
		}
	}

	public final ArgsContext args() throws RecognitionException {
		ArgsContext _localctx = new ArgsContext(_ctx, getState());
		enterRule(_localctx, 20, RULE_args);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(118);
			arg();
			setState(123);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==COMMA) {
				{
				{
				setState(119);
				match(COMMA);
				setState(120);
				arg();
				}
				}
				setState(125);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class ArgContext extends ParserRuleContext {
		public ExprContext expr() {
			return getRuleContext(ExprContext.class,0);
		}
		public ExpressionMarkerContext expressionMarker() {
			return getRuleContext(ExpressionMarkerContext.class,0);
		}
		public ArgContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_arg; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterArg(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitArg(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitArg(this);
			else return visitor.visitChildren(this);
		}
	}

	public final ArgContext arg() throws RecognitionException {
		ArgContext _localctx = new ArgContext(_ctx, getState());
		enterRule(_localctx, 22, RULE_arg);
		try {
			setState(128);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,13,_ctx) ) {
			case 1:
				enterOuterAlt(_localctx, 1);
				{
				setState(126);
				expr(0);
				}
				break;
			case 2:
				enterOuterAlt(_localctx, 2);
				{
				setState(127);
				expressionMarker();
				}
				break;
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class BlockContext extends ParserRuleContext {
		public TerminalNode CURLYBRACE_OPEN() { return getToken(ElsaParser.CURLYBRACE_OPEN, 0); }
		public TerminalNode CURLYBRACE_CLOSE() { return getToken(ElsaParser.CURLYBRACE_CLOSE, 0); }
		public List<StatContext> stat() {
			return getRuleContexts(StatContext.class);
		}
		public StatContext stat(int i) {
			return getRuleContext(StatContext.class,i);
		}
		public BlockContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_block; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterBlock(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitBlock(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitBlock(this);
			else return visitor.visitChildren(this);
		}
	}

	public final BlockContext block() throws RecognitionException {
		BlockContext _localctx = new BlockContext(_ctx, getState());
		enterRule(_localctx, 24, RULE_block);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(130);
			match(CURLYBRACE_OPEN);
			setState(134);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while ((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << NEW) | (1L << VARIABLE) | (1L << LET) | (1L << IF) | (1L << FOR) | (1L << RETURN) | (1L << MINUS) | (1L << PARENTHESES_OPEN) | (1L << BRACKET_OPEN) | (1L << CURLYBRACE_OPEN) | (1L << EXCLAMATION) | (1L << STRING_VAL) | (1L << BACKTICKSTRING_VAL) | (1L << INTEGER_VAL) | (1L << ID))) != 0)) {
				{
				{
				setState(131);
				stat();
				}
				}
				setState(136);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			setState(137);
			match(CURLYBRACE_CLOSE);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class ObjectInitializerContext extends ParserRuleContext {
		public TerminalNode CURLYBRACE_OPEN() { return getToken(ElsaParser.CURLYBRACE_OPEN, 0); }
		public TerminalNode CURLYBRACE_CLOSE() { return getToken(ElsaParser.CURLYBRACE_CLOSE, 0); }
		public PropertyListContext propertyList() {
			return getRuleContext(PropertyListContext.class,0);
		}
		public ObjectInitializerContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_objectInitializer; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterObjectInitializer(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitObjectInitializer(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitObjectInitializer(this);
			else return visitor.visitChildren(this);
		}
	}

	public final ObjectInitializerContext objectInitializer() throws RecognitionException {
		ObjectInitializerContext _localctx = new ObjectInitializerContext(_ctx, getState());
		enterRule(_localctx, 26, RULE_objectInitializer);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(139);
			match(CURLYBRACE_OPEN);
			setState(141);
			_errHandler.sync(this);
			_la = _input.LA(1);
			if (_la==ID) {
				{
				setState(140);
				propertyList();
				}
			}

			setState(143);
			match(CURLYBRACE_CLOSE);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class PropertyListContext extends ParserRuleContext {
		public List<PropertyContext> property() {
			return getRuleContexts(PropertyContext.class);
		}
		public PropertyContext property(int i) {
			return getRuleContext(PropertyContext.class,i);
		}
		public List<TerminalNode> COMMA() { return getTokens(ElsaParser.COMMA); }
		public TerminalNode COMMA(int i) {
			return getToken(ElsaParser.COMMA, i);
		}
		public PropertyListContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_propertyList; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterPropertyList(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitPropertyList(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitPropertyList(this);
			else return visitor.visitChildren(this);
		}
	}

	public final PropertyListContext propertyList() throws RecognitionException {
		PropertyListContext _localctx = new PropertyListContext(_ctx, getState());
		enterRule(_localctx, 28, RULE_propertyList);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(145);
			property();
			setState(150);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==COMMA) {
				{
				{
				setState(146);
				match(COMMA);
				setState(147);
				property();
				}
				}
				setState(152);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class PropertyContext extends ParserRuleContext {
		public TerminalNode ID() { return getToken(ElsaParser.ID, 0); }
		public TerminalNode COLON() { return getToken(ElsaParser.COLON, 0); }
		public ExprContext expr() {
			return getRuleContext(ExprContext.class,0);
		}
		public PropertyContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_property; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterProperty(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitProperty(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitProperty(this);
			else return visitor.visitChildren(this);
		}
	}

	public final PropertyContext property() throws RecognitionException {
		PropertyContext _localctx = new PropertyContext(_ctx, getState());
		enterRule(_localctx, 30, RULE_property);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(153);
			match(ID);
			setState(154);
			match(COLON);
			setState(155);
			expr(0);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class StatContext extends ParserRuleContext {
		public StatContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_stat; }
	 
		public StatContext() { }
		public void copyFrom(StatContext ctx) {
			super.copyFrom(ctx);
		}
	}
	public static class IfStatContext extends StatContext {
		public TerminalNode IF() { return getToken(ElsaParser.IF, 0); }
		public ExprContext expr() {
			return getRuleContext(ExprContext.class,0);
		}
		public TerminalNode THEN() { return getToken(ElsaParser.THEN, 0); }
		public ThenStatContext thenStat() {
			return getRuleContext(ThenStatContext.class,0);
		}
		public TerminalNode ELSE() { return getToken(ElsaParser.ELSE, 0); }
		public ElseStatContext elseStat() {
			return getRuleContext(ElseStatContext.class,0);
		}
		public IfStatContext(StatContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterIfStat(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitIfStat(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitIfStat(this);
			else return visitor.visitChildren(this);
		}
	}
	public static class BlockStatContext extends StatContext {
		public BlockContext block() {
			return getRuleContext(BlockContext.class,0);
		}
		public BlockStatContext(StatContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterBlockStat(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitBlockStat(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitBlockStat(this);
			else return visitor.visitChildren(this);
		}
	}
	public static class ExpressionStatContext extends StatContext {
		public ExprContext expr() {
			return getRuleContext(ExprContext.class,0);
		}
		public TerminalNode SEMICOLON() { return getToken(ElsaParser.SEMICOLON, 0); }
		public ExpressionStatContext(StatContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterExpressionStat(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitExpressionStat(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitExpressionStat(this);
			else return visitor.visitChildren(this);
		}
	}
	public static class ObjectStatContext extends StatContext {
		public ObjectContext object() {
			return getRuleContext(ObjectContext.class,0);
		}
		public TerminalNode SEMICOLON() { return getToken(ElsaParser.SEMICOLON, 0); }
		public ObjectStatContext(StatContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterObjectStat(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitObjectStat(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitObjectStat(this);
			else return visitor.visitChildren(this);
		}
	}
	public static class ReturnStatContext extends StatContext {
		public TerminalNode RETURN() { return getToken(ElsaParser.RETURN, 0); }
		public TerminalNode SEMICOLON() { return getToken(ElsaParser.SEMICOLON, 0); }
		public ExprContext expr() {
			return getRuleContext(ExprContext.class,0);
		}
		public ReturnStatContext(StatContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterReturnStat(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitReturnStat(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitReturnStat(this);
			else return visitor.visitChildren(this);
		}
	}
	public static class VariableDeclarationStatContext extends StatContext {
		public VarDeclContext varDecl() {
			return getRuleContext(VarDeclContext.class,0);
		}
		public TerminalNode SEMICOLON() { return getToken(ElsaParser.SEMICOLON, 0); }
		public VariableDeclarationStatContext(StatContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterVariableDeclarationStat(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitVariableDeclarationStat(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitVariableDeclarationStat(this);
			else return visitor.visitChildren(this);
		}
	}
	public static class AssignmentStatContext extends StatContext {
		public List<ExprContext> expr() {
			return getRuleContexts(ExprContext.class);
		}
		public ExprContext expr(int i) {
			return getRuleContext(ExprContext.class,i);
		}
		public TerminalNode EQ() { return getToken(ElsaParser.EQ, 0); }
		public TerminalNode SEMICOLON() { return getToken(ElsaParser.SEMICOLON, 0); }
		public AssignmentStatContext(StatContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterAssignmentStat(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitAssignmentStat(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitAssignmentStat(this);
			else return visitor.visitChildren(this);
		}
	}
	public static class LocalVariableDeclarationStatContext extends StatContext {
		public LocalVarDeclContext localVarDecl() {
			return getRuleContext(LocalVarDeclContext.class,0);
		}
		public TerminalNode SEMICOLON() { return getToken(ElsaParser.SEMICOLON, 0); }
		public LocalVariableDeclarationStatContext(StatContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterLocalVariableDeclarationStat(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitLocalVariableDeclarationStat(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitLocalVariableDeclarationStat(this);
			else return visitor.visitChildren(this);
		}
	}
	public static class ForStatContext extends StatContext {
		public TerminalNode FOR() { return getToken(ElsaParser.FOR, 0); }
		public TerminalNode PARENTHESES_OPEN() { return getToken(ElsaParser.PARENTHESES_OPEN, 0); }
		public TerminalNode ID() { return getToken(ElsaParser.ID, 0); }
		public TerminalNode EQ() { return getToken(ElsaParser.EQ, 0); }
		public List<ExprContext> expr() {
			return getRuleContexts(ExprContext.class);
		}
		public ExprContext expr(int i) {
			return getRuleContext(ExprContext.class,i);
		}
		public List<TerminalNode> SEMICOLON() { return getTokens(ElsaParser.SEMICOLON); }
		public TerminalNode SEMICOLON(int i) {
			return getToken(ElsaParser.SEMICOLON, i);
		}
		public TerminalNode PARENTHESES_CLOSE() { return getToken(ElsaParser.PARENTHESES_CLOSE, 0); }
		public StatContext stat() {
			return getRuleContext(StatContext.class,0);
		}
		public ForStatContext(StatContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterForStat(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitForStat(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitForStat(this);
			else return visitor.visitChildren(this);
		}
	}

	public final StatContext stat() throws RecognitionException {
		StatContext _localctx = new StatContext(_ctx, getState());
		enterRule(_localctx, 32, RULE_stat);
		int _la;
		try {
			setState(200);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,19,_ctx) ) {
			case 1:
				_localctx = new ObjectStatContext(_localctx);
				enterOuterAlt(_localctx, 1);
				{
				setState(157);
				object();
				setState(158);
				match(SEMICOLON);
				}
				break;
			case 2:
				_localctx = new IfStatContext(_localctx);
				enterOuterAlt(_localctx, 2);
				{
				setState(160);
				match(IF);
				setState(161);
				expr(0);
				setState(162);
				match(THEN);
				setState(163);
				thenStat();
				setState(166);
				_errHandler.sync(this);
				switch ( getInterpreter().adaptivePredict(_input,17,_ctx) ) {
				case 1:
					{
					setState(164);
					match(ELSE);
					setState(165);
					elseStat();
					}
					break;
				}
				}
				break;
			case 3:
				_localctx = new ForStatContext(_localctx);
				enterOuterAlt(_localctx, 3);
				{
				setState(168);
				match(FOR);
				setState(169);
				match(PARENTHESES_OPEN);
				setState(170);
				match(ID);
				setState(171);
				match(EQ);
				setState(172);
				expr(0);
				setState(173);
				match(SEMICOLON);
				setState(174);
				expr(0);
				setState(175);
				match(SEMICOLON);
				setState(176);
				expr(0);
				setState(177);
				match(PARENTHESES_CLOSE);
				setState(178);
				stat();
				}
				break;
			case 4:
				_localctx = new ReturnStatContext(_localctx);
				enterOuterAlt(_localctx, 4);
				{
				setState(180);
				match(RETURN);
				setState(182);
				_errHandler.sync(this);
				_la = _input.LA(1);
				if ((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << NEW) | (1L << MINUS) | (1L << PARENTHESES_OPEN) | (1L << BRACKET_OPEN) | (1L << EXCLAMATION) | (1L << STRING_VAL) | (1L << BACKTICKSTRING_VAL) | (1L << INTEGER_VAL) | (1L << ID))) != 0)) {
					{
					setState(181);
					expr(0);
					}
				}

				setState(184);
				match(SEMICOLON);
				}
				break;
			case 5:
				_localctx = new BlockStatContext(_localctx);
				enterOuterAlt(_localctx, 5);
				{
				setState(185);
				block();
				}
				break;
			case 6:
				_localctx = new VariableDeclarationStatContext(_localctx);
				enterOuterAlt(_localctx, 6);
				{
				setState(186);
				varDecl();
				setState(187);
				match(SEMICOLON);
				}
				break;
			case 7:
				_localctx = new LocalVariableDeclarationStatContext(_localctx);
				enterOuterAlt(_localctx, 7);
				{
				setState(189);
				localVarDecl();
				setState(190);
				match(SEMICOLON);
				}
				break;
			case 8:
				_localctx = new AssignmentStatContext(_localctx);
				enterOuterAlt(_localctx, 8);
				{
				setState(192);
				expr(0);
				setState(193);
				match(EQ);
				setState(194);
				expr(0);
				setState(195);
				match(SEMICOLON);
				}
				break;
			case 9:
				_localctx = new ExpressionStatContext(_localctx);
				enterOuterAlt(_localctx, 9);
				{
				setState(197);
				expr(0);
				setState(198);
				match(SEMICOLON);
				}
				break;
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class ThenStatContext extends ParserRuleContext {
		public StatContext stat() {
			return getRuleContext(StatContext.class,0);
		}
		public ThenStatContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_thenStat; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterThenStat(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitThenStat(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitThenStat(this);
			else return visitor.visitChildren(this);
		}
	}

	public final ThenStatContext thenStat() throws RecognitionException {
		ThenStatContext _localctx = new ThenStatContext(_ctx, getState());
		enterRule(_localctx, 34, RULE_thenStat);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(202);
			stat();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class ElseStatContext extends ParserRuleContext {
		public StatContext stat() {
			return getRuleContext(StatContext.class,0);
		}
		public ElseStatContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_elseStat; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterElseStat(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitElseStat(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitElseStat(this);
			else return visitor.visitChildren(this);
		}
	}

	public final ElseStatContext elseStat() throws RecognitionException {
		ElseStatContext _localctx = new ElseStatContext(_ctx, getState());
		enterRule(_localctx, 36, RULE_elseStat);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(204);
			stat();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class ExprContext extends ParserRuleContext {
		public ExprContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_expr; }
	 
		public ExprContext() { }
		public void copyFrom(ExprContext ctx) {
			super.copyFrom(ctx);
		}
	}
	public static class NewObjectExprContext extends ExprContext {
		public NewObjectContext newObject() {
			return getRuleContext(NewObjectContext.class,0);
		}
		public NewObjectExprContext(ExprContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterNewObjectExpr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitNewObjectExpr(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitNewObjectExpr(this);
			else return visitor.visitChildren(this);
		}
	}
	public static class SubtractExprContext extends ExprContext {
		public List<ExprContext> expr() {
			return getRuleContexts(ExprContext.class);
		}
		public ExprContext expr(int i) {
			return getRuleContext(ExprContext.class,i);
		}
		public TerminalNode MINUS() { return getToken(ElsaParser.MINUS, 0); }
		public SubtractExprContext(ExprContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterSubtractExpr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitSubtractExpr(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitSubtractExpr(this);
			else return visitor.visitChildren(this);
		}
	}
	public static class IncrementExprContext extends ExprContext {
		public ExprContext expr() {
			return getRuleContext(ExprContext.class,0);
		}
		public TerminalNode INCREMENT() { return getToken(ElsaParser.INCREMENT, 0); }
		public IncrementExprContext(ExprContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterIncrementExpr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitIncrementExpr(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitIncrementExpr(this);
			else return visitor.visitChildren(this);
		}
	}
	public static class ObjectExprContext extends ExprContext {
		public ObjectContext object() {
			return getRuleContext(ObjectContext.class,0);
		}
		public ObjectExprContext(ExprContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterObjectExpr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitObjectExpr(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitObjectExpr(this);
			else return visitor.visitChildren(this);
		}
	}
	public static class StringValueExprContext extends ExprContext {
		public TerminalNode STRING_VAL() { return getToken(ElsaParser.STRING_VAL, 0); }
		public StringValueExprContext(ExprContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterStringValueExpr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitStringValueExpr(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitStringValueExpr(this);
			else return visitor.visitChildren(this);
		}
	}
	public static class MultiplyExprContext extends ExprContext {
		public List<ExprContext> expr() {
			return getRuleContexts(ExprContext.class);
		}
		public ExprContext expr(int i) {
			return getRuleContext(ExprContext.class,i);
		}
		public TerminalNode STAR() { return getToken(ElsaParser.STAR, 0); }
		public MultiplyExprContext(ExprContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterMultiplyExpr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitMultiplyExpr(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitMultiplyExpr(this);
			else return visitor.visitChildren(this);
		}
	}
	public static class ParenthesesExprContext extends ExprContext {
		public TerminalNode PARENTHESES_OPEN() { return getToken(ElsaParser.PARENTHESES_OPEN, 0); }
		public TerminalNode PARENTHESES_CLOSE() { return getToken(ElsaParser.PARENTHESES_CLOSE, 0); }
		public ExprListContext exprList() {
			return getRuleContext(ExprListContext.class,0);
		}
		public ParenthesesExprContext(ExprContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterParenthesesExpr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitParenthesesExpr(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitParenthesesExpr(this);
			else return visitor.visitChildren(this);
		}
	}
	public static class FunctionExprContext extends ExprContext {
		public FuncCallContext funcCall() {
			return getRuleContext(FuncCallContext.class,0);
		}
		public FunctionExprContext(ExprContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterFunctionExpr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitFunctionExpr(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitFunctionExpr(this);
			else return visitor.visitChildren(this);
		}
	}
	public static class DecrementExprContext extends ExprContext {
		public ExprContext expr() {
			return getRuleContext(ExprContext.class,0);
		}
		public TerminalNode DECREMENT() { return getToken(ElsaParser.DECREMENT, 0); }
		public DecrementExprContext(ExprContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterDecrementExpr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitDecrementExpr(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitDecrementExpr(this);
			else return visitor.visitChildren(this);
		}
	}
	public static class NegateExprContext extends ExprContext {
		public TerminalNode MINUS() { return getToken(ElsaParser.MINUS, 0); }
		public ExprContext expr() {
			return getRuleContext(ExprContext.class,0);
		}
		public NegateExprContext(ExprContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterNegateExpr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitNegateExpr(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitNegateExpr(this);
			else return visitor.visitChildren(this);
		}
	}
	public static class MethodCallExprContext extends ExprContext {
		public MethodCallContext methodCall() {
			return getRuleContext(MethodCallContext.class,0);
		}
		public MethodCallExprContext(ExprContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterMethodCallExpr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitMethodCallExpr(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitMethodCallExpr(this);
			else return visitor.visitChildren(this);
		}
	}
	public static class VariableExprContext extends ExprContext {
		public TerminalNode ID() { return getToken(ElsaParser.ID, 0); }
		public VariableExprContext(ExprContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterVariableExpr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitVariableExpr(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitVariableExpr(this);
			else return visitor.visitChildren(this);
		}
	}
	public static class NotExprContext extends ExprContext {
		public TerminalNode EXCLAMATION() { return getToken(ElsaParser.EXCLAMATION, 0); }
		public ExprContext expr() {
			return getRuleContext(ExprContext.class,0);
		}
		public NotExprContext(ExprContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterNotExpr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitNotExpr(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitNotExpr(this);
			else return visitor.visitChildren(this);
		}
	}
	public static class IntegerValueExprContext extends ExprContext {
		public TerminalNode INTEGER_VAL() { return getToken(ElsaParser.INTEGER_VAL, 0); }
		public IntegerValueExprContext(ExprContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterIntegerValueExpr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitIntegerValueExpr(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitIntegerValueExpr(this);
			else return visitor.visitChildren(this);
		}
	}
	public static class AddExprContext extends ExprContext {
		public List<ExprContext> expr() {
			return getRuleContexts(ExprContext.class);
		}
		public ExprContext expr(int i) {
			return getRuleContext(ExprContext.class,i);
		}
		public TerminalNode PLUS() { return getToken(ElsaParser.PLUS, 0); }
		public AddExprContext(ExprContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterAddExpr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitAddExpr(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitAddExpr(this);
			else return visitor.visitChildren(this);
		}
	}
	public static class BackTickStringValueExprContext extends ExprContext {
		public TerminalNode BACKTICKSTRING_VAL() { return getToken(ElsaParser.BACKTICKSTRING_VAL, 0); }
		public BackTickStringValueExprContext(ExprContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterBackTickStringValueExpr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitBackTickStringValueExpr(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitBackTickStringValueExpr(this);
			else return visitor.visitChildren(this);
		}
	}
	public static class BracketsExprContext extends ExprContext {
		public TerminalNode BRACKET_OPEN() { return getToken(ElsaParser.BRACKET_OPEN, 0); }
		public TerminalNode BRACKET_CLOSE() { return getToken(ElsaParser.BRACKET_CLOSE, 0); }
		public ExprListContext exprList() {
			return getRuleContext(ExprListContext.class,0);
		}
		public BracketsExprContext(ExprContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterBracketsExpr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitBracketsExpr(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitBracketsExpr(this);
			else return visitor.visitChildren(this);
		}
	}
	public static class CompareExprContext extends ExprContext {
		public List<ExprContext> expr() {
			return getRuleContexts(ExprContext.class);
		}
		public ExprContext expr(int i) {
			return getRuleContext(ExprContext.class,i);
		}
		public TerminalNode EQUALS() { return getToken(ElsaParser.EQUALS, 0); }
		public TerminalNode GREATER() { return getToken(ElsaParser.GREATER, 0); }
		public TerminalNode LESS() { return getToken(ElsaParser.LESS, 0); }
		public CompareExprContext(ExprContext ctx) { copyFrom(ctx); }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterCompareExpr(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitCompareExpr(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitCompareExpr(this);
			else return visitor.visitChildren(this);
		}
	}

	public final ExprContext expr() throws RecognitionException {
		return expr(0);
	}

	private ExprContext expr(int _p) throws RecognitionException {
		ParserRuleContext _parentctx = _ctx;
		int _parentState = getState();
		ExprContext _localctx = new ExprContext(_ctx, _parentState);
		ExprContext _prevctx = _localctx;
		int _startState = 38;
		enterRecursionRule(_localctx, 38, RULE_expr, _p);
		int _la;
		try {
			int _alt;
			enterOuterAlt(_localctx, 1);
			{
			setState(229);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,22,_ctx) ) {
			case 1:
				{
				_localctx = new FunctionExprContext(_localctx);
				_ctx = _localctx;
				_prevctx = _localctx;

				setState(207);
				funcCall();
				}
				break;
			case 2:
				{
				_localctx = new ObjectExprContext(_localctx);
				_ctx = _localctx;
				_prevctx = _localctx;
				setState(208);
				object();
				}
				break;
			case 3:
				{
				_localctx = new NewObjectExprContext(_localctx);
				_ctx = _localctx;
				_prevctx = _localctx;
				setState(209);
				newObject();
				}
				break;
			case 4:
				{
				_localctx = new NegateExprContext(_localctx);
				_ctx = _localctx;
				_prevctx = _localctx;
				setState(210);
				match(MINUS);
				setState(211);
				expr(13);
				}
				break;
			case 5:
				{
				_localctx = new NotExprContext(_localctx);
				_ctx = _localctx;
				_prevctx = _localctx;
				setState(212);
				match(EXCLAMATION);
				setState(213);
				expr(12);
				}
				break;
			case 6:
				{
				_localctx = new IntegerValueExprContext(_localctx);
				_ctx = _localctx;
				_prevctx = _localctx;
				setState(214);
				match(INTEGER_VAL);
				}
				break;
			case 7:
				{
				_localctx = new StringValueExprContext(_localctx);
				_ctx = _localctx;
				_prevctx = _localctx;
				setState(215);
				match(STRING_VAL);
				}
				break;
			case 8:
				{
				_localctx = new BackTickStringValueExprContext(_localctx);
				_ctx = _localctx;
				_prevctx = _localctx;
				setState(216);
				match(BACKTICKSTRING_VAL);
				}
				break;
			case 9:
				{
				_localctx = new ParenthesesExprContext(_localctx);
				_ctx = _localctx;
				_prevctx = _localctx;
				setState(217);
				match(PARENTHESES_OPEN);
				setState(219);
				_errHandler.sync(this);
				_la = _input.LA(1);
				if ((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << NEW) | (1L << MINUS) | (1L << PARENTHESES_OPEN) | (1L << BRACKET_OPEN) | (1L << EXCLAMATION) | (1L << STRING_VAL) | (1L << BACKTICKSTRING_VAL) | (1L << INTEGER_VAL) | (1L << ID))) != 0)) {
					{
					setState(218);
					exprList();
					}
				}

				setState(221);
				match(PARENTHESES_CLOSE);
				}
				break;
			case 10:
				{
				_localctx = new BracketsExprContext(_localctx);
				_ctx = _localctx;
				_prevctx = _localctx;
				setState(222);
				match(BRACKET_OPEN);
				setState(224);
				_errHandler.sync(this);
				_la = _input.LA(1);
				if ((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << NEW) | (1L << MINUS) | (1L << PARENTHESES_OPEN) | (1L << BRACKET_OPEN) | (1L << EXCLAMATION) | (1L << STRING_VAL) | (1L << BACKTICKSTRING_VAL) | (1L << INTEGER_VAL) | (1L << ID))) != 0)) {
					{
					setState(223);
					exprList();
					}
				}

				setState(226);
				match(BRACKET_CLOSE);
				}
				break;
			case 11:
				{
				_localctx = new MethodCallExprContext(_localctx);
				_ctx = _localctx;
				_prevctx = _localctx;
				setState(227);
				methodCall();
				}
				break;
			case 12:
				{
				_localctx = new VariableExprContext(_localctx);
				_ctx = _localctx;
				_prevctx = _localctx;
				setState(228);
				match(ID);
				}
				break;
			}
			_ctx.stop = _input.LT(-1);
			setState(249);
			_errHandler.sync(this);
			_alt = getInterpreter().adaptivePredict(_input,24,_ctx);
			while ( _alt!=2 && _alt!=org.antlr.v4.runtime.atn.ATN.INVALID_ALT_NUMBER ) {
				if ( _alt==1 ) {
					if ( _parseListeners!=null ) triggerExitRuleEvent();
					_prevctx = _localctx;
					{
					setState(247);
					_errHandler.sync(this);
					switch ( getInterpreter().adaptivePredict(_input,23,_ctx) ) {
					case 1:
						{
						_localctx = new MultiplyExprContext(new ExprContext(_parentctx, _parentState));
						pushNewRecursionContext(_localctx, _startState, RULE_expr);
						setState(231);
						if (!(precpred(_ctx, 11))) throw new FailedPredicateException(this, "precpred(_ctx, 11)");
						setState(232);
						match(STAR);
						setState(233);
						expr(12);
						}
						break;
					case 2:
						{
						_localctx = new AddExprContext(new ExprContext(_parentctx, _parentState));
						pushNewRecursionContext(_localctx, _startState, RULE_expr);
						setState(234);
						if (!(precpred(_ctx, 10))) throw new FailedPredicateException(this, "precpred(_ctx, 10)");
						setState(235);
						match(PLUS);
						setState(236);
						expr(11);
						}
						break;
					case 3:
						{
						_localctx = new SubtractExprContext(new ExprContext(_parentctx, _parentState));
						pushNewRecursionContext(_localctx, _startState, RULE_expr);
						setState(237);
						if (!(precpred(_ctx, 9))) throw new FailedPredicateException(this, "precpred(_ctx, 9)");
						setState(238);
						match(MINUS);
						setState(239);
						expr(10);
						}
						break;
					case 4:
						{
						_localctx = new CompareExprContext(new ExprContext(_parentctx, _parentState));
						pushNewRecursionContext(_localctx, _startState, RULE_expr);
						setState(240);
						if (!(precpred(_ctx, 8))) throw new FailedPredicateException(this, "precpred(_ctx, 8)");
						setState(241);
						_la = _input.LA(1);
						if ( !((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << GREATER) | (1L << EQUALS) | (1L << LESS))) != 0)) ) {
						_errHandler.recoverInline(this);
						}
						else {
							if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
							_errHandler.reportMatch(this);
							consume();
						}
						setState(242);
						expr(9);
						}
						break;
					case 5:
						{
						_localctx = new IncrementExprContext(new ExprContext(_parentctx, _parentState));
						pushNewRecursionContext(_localctx, _startState, RULE_expr);
						setState(243);
						if (!(precpred(_ctx, 15))) throw new FailedPredicateException(this, "precpred(_ctx, 15)");
						setState(244);
						match(INCREMENT);
						}
						break;
					case 6:
						{
						_localctx = new DecrementExprContext(new ExprContext(_parentctx, _parentState));
						pushNewRecursionContext(_localctx, _startState, RULE_expr);
						setState(245);
						if (!(precpred(_ctx, 14))) throw new FailedPredicateException(this, "precpred(_ctx, 14)");
						setState(246);
						match(DECREMENT);
						}
						break;
					}
					} 
				}
				setState(251);
				_errHandler.sync(this);
				_alt = getInterpreter().adaptivePredict(_input,24,_ctx);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			unrollRecursionContexts(_parentctx);
		}
		return _localctx;
	}

	public static class ExprListContext extends ParserRuleContext {
		public List<ExprContext> expr() {
			return getRuleContexts(ExprContext.class);
		}
		public ExprContext expr(int i) {
			return getRuleContext(ExprContext.class,i);
		}
		public List<TerminalNode> COMMA() { return getTokens(ElsaParser.COMMA); }
		public TerminalNode COMMA(int i) {
			return getToken(ElsaParser.COMMA, i);
		}
		public ExprListContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_exprList; }
		@Override
		public void enterRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).enterExprList(this);
		}
		@Override
		public void exitRule(ParseTreeListener listener) {
			if ( listener instanceof ElsaParserListener ) ((ElsaParserListener)listener).exitExprList(this);
		}
		@Override
		public <T> T accept(ParseTreeVisitor<? extends T> visitor) {
			if ( visitor instanceof ElsaParserVisitor ) return ((ElsaParserVisitor<? extends T>)visitor).visitExprList(this);
			else return visitor.visitChildren(this);
		}
	}

	public final ExprListContext exprList() throws RecognitionException {
		ExprListContext _localctx = new ExprListContext(_ctx, getState());
		enterRule(_localctx, 40, RULE_exprList);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(252);
			expr(0);
			setState(257);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==COMMA) {
				{
				{
				setState(253);
				match(COMMA);
				setState(254);
				expr(0);
				}
				}
				setState(259);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public boolean sempred(RuleContext _localctx, int ruleIndex, int predIndex) {
		switch (ruleIndex) {
		case 19:
			return expr_sempred((ExprContext)_localctx, predIndex);
		}
		return true;
	}
	private boolean expr_sempred(ExprContext _localctx, int predIndex) {
		switch (predIndex) {
		case 0:
			return precpred(_ctx, 11);
		case 1:
			return precpred(_ctx, 10);
		case 2:
			return precpred(_ctx, 9);
		case 3:
			return precpred(_ctx, 8);
		case 4:
			return precpred(_ctx, 15);
		case 5:
			return precpred(_ctx, 14);
		}
		return true;
	}

	public static final String _serializedATN =
		"\3\u608b\ua72a\u8133\ub9ed\u417c\u3be7\u7786\u5964\3\62\u0107\4\2\t\2"+
		"\4\3\t\3\4\4\t\4\4\5\t\5\4\6\t\6\4\7\t\7\4\b\t\b\4\t\t\t\4\n\t\n\4\13"+
		"\t\13\4\f\t\f\4\r\t\r\4\16\t\16\4\17\t\17\4\20\t\20\4\21\t\21\4\22\t\22"+
		"\4\23\t\23\4\24\t\24\4\25\t\25\4\26\t\26\3\2\3\2\7\2/\n\2\f\2\16\2\62"+
		"\13\2\3\3\3\3\5\3\66\n\3\3\4\3\4\3\4\3\4\3\4\3\4\5\4>\n\4\3\4\3\4\5\4"+
		"B\n\4\3\4\3\4\3\5\3\5\3\5\3\5\5\5J\n\5\3\5\3\5\5\5N\n\5\3\6\3\6\3\6\3"+
		"\6\5\6T\n\6\3\6\3\6\5\6X\n\6\3\7\3\7\3\b\3\b\3\b\3\b\3\b\3\b\3\b\3\b\3"+
		"\b\3\b\5\bf\n\b\3\t\7\ti\n\t\f\t\16\tl\13\t\3\n\3\n\3\n\3\n\3\13\3\13"+
		"\3\13\5\13u\n\13\3\13\3\13\3\f\3\f\3\f\7\f|\n\f\f\f\16\f\177\13\f\3\r"+
		"\3\r\5\r\u0083\n\r\3\16\3\16\7\16\u0087\n\16\f\16\16\16\u008a\13\16\3"+
		"\16\3\16\3\17\3\17\5\17\u0090\n\17\3\17\3\17\3\20\3\20\3\20\7\20\u0097"+
		"\n\20\f\20\16\20\u009a\13\20\3\21\3\21\3\21\3\21\3\22\3\22\3\22\3\22\3"+
		"\22\3\22\3\22\3\22\3\22\5\22\u00a9\n\22\3\22\3\22\3\22\3\22\3\22\3\22"+
		"\3\22\3\22\3\22\3\22\3\22\3\22\3\22\3\22\5\22\u00b9\n\22\3\22\3\22\3\22"+
		"\3\22\3\22\3\22\3\22\3\22\3\22\3\22\3\22\3\22\3\22\3\22\3\22\3\22\5\22"+
		"\u00cb\n\22\3\23\3\23\3\24\3\24\3\25\3\25\3\25\3\25\3\25\3\25\3\25\3\25"+
		"\3\25\3\25\3\25\3\25\3\25\5\25\u00de\n\25\3\25\3\25\3\25\5\25\u00e3\n"+
		"\25\3\25\3\25\3\25\5\25\u00e8\n\25\3\25\3\25\3\25\3\25\3\25\3\25\3\25"+
		"\3\25\3\25\3\25\3\25\3\25\3\25\3\25\3\25\3\25\7\25\u00fa\n\25\f\25\16"+
		"\25\u00fd\13\25\3\26\3\26\3\26\7\26\u0102\n\26\f\26\16\26\u0105\13\26"+
		"\3\26\3j\3(\27\2\4\6\b\n\f\16\20\22\24\26\30\32\34\36 \"$&(*\2\4\4\2\17"+
		"\23\60\60\5\2\4\4\34\34\37\37\2\u0120\2\60\3\2\2\2\4\63\3\2\2\2\6\67\3"+
		"\2\2\2\bE\3\2\2\2\nO\3\2\2\2\fY\3\2\2\2\16e\3\2\2\2\20j\3\2\2\2\22m\3"+
		"\2\2\2\24q\3\2\2\2\26x\3\2\2\2\30\u0082\3\2\2\2\32\u0084\3\2\2\2\34\u008d"+
		"\3\2\2\2\36\u0093\3\2\2\2 \u009b\3\2\2\2\"\u00ca\3\2\2\2$\u00cc\3\2\2"+
		"\2&\u00ce\3\2\2\2(\u00e7\3\2\2\2*\u00fe\3\2\2\2,/\5\"\22\2-/\7.\2\2.,"+
		"\3\2\2\2.-\3\2\2\2/\62\3\2\2\2\60.\3\2\2\2\60\61\3\2\2\2\61\3\3\2\2\2"+
		"\62\60\3\2\2\2\63\65\7\60\2\2\64\66\5\34\17\2\65\64\3\2\2\2\65\66\3\2"+
		"\2\2\66\5\3\2\2\2\678\7\7\2\28=\7\60\2\29:\7\37\2\2:;\5\f\7\2;<\7\4\2"+
		"\2<>\3\2\2\2=9\3\2\2\2=>\3\2\2\2>?\3\2\2\2?A\7\"\2\2@B\5\26\f\2A@\3\2"+
		"\2\2AB\3\2\2\2BC\3\2\2\2CD\7#\2\2D\7\3\2\2\2EF\7\b\2\2FI\7\60\2\2GH\7"+
		"\26\2\2HJ\5\f\7\2IG\3\2\2\2IJ\3\2\2\2JM\3\2\2\2KL\7\3\2\2LN\5(\25\2MK"+
		"\3\2\2\2MN\3\2\2\2N\t\3\2\2\2OP\7\t\2\2PS\7\60\2\2QR\7\26\2\2RT\5\f\7"+
		"\2SQ\3\2\2\2ST\3\2\2\2TW\3\2\2\2UV\7\3\2\2VX\5(\25\2WU\3\2\2\2WX\3\2\2"+
		"\2X\13\3\2\2\2YZ\t\2\2\2Z\r\3\2\2\2[\\\7\24\2\2\\]\7\"\2\2]^\7\60\2\2"+
		"^_\7\30\2\2_`\5\20\t\2`a\7#\2\2af\3\2\2\2bc\7\60\2\2cd\7!\2\2df\5\20\t"+
		"\2e[\3\2\2\2eb\3\2\2\2f\17\3\2\2\2gi\13\2\2\2hg\3\2\2\2il\3\2\2\2jk\3"+
		"\2\2\2jh\3\2\2\2k\21\3\2\2\2lj\3\2\2\2mn\7\60\2\2no\7+\2\2op\5\24\13\2"+
		"p\23\3\2\2\2qr\7\60\2\2rt\7\"\2\2su\5\26\f\2ts\3\2\2\2tu\3\2\2\2uv\3\2"+
		"\2\2vw\7#\2\2w\25\3\2\2\2x}\5\30\r\2yz\7\30\2\2z|\5\30\r\2{y\3\2\2\2|"+
		"\177\3\2\2\2}{\3\2\2\2}~\3\2\2\2~\27\3\2\2\2\177}\3\2\2\2\u0080\u0083"+
		"\5(\25\2\u0081\u0083\5\16\b\2\u0082\u0080\3\2\2\2\u0082\u0081\3\2\2\2"+
		"\u0083\31\3\2\2\2\u0084\u0088\7&\2\2\u0085\u0087\5\"\22\2\u0086\u0085"+
		"\3\2\2\2\u0087\u008a\3\2\2\2\u0088\u0086\3\2\2\2\u0088\u0089\3\2\2\2\u0089"+
		"\u008b\3\2\2\2\u008a\u0088\3\2\2\2\u008b\u008c\7\'\2\2\u008c\33\3\2\2"+
		"\2\u008d\u008f\7&\2\2\u008e\u0090\5\36\20\2\u008f\u008e\3\2\2\2\u008f"+
		"\u0090\3\2\2\2\u0090\u0091\3\2\2\2\u0091\u0092\7\'\2\2\u0092\35\3\2\2"+
		"\2\u0093\u0098\5 \21\2\u0094\u0095\7\30\2\2\u0095\u0097\5 \21\2\u0096"+
		"\u0094\3\2\2\2\u0097\u009a\3\2\2\2\u0098\u0096\3\2\2\2\u0098\u0099\3\2"+
		"\2\2\u0099\37\3\2\2\2\u009a\u0098\3\2\2\2\u009b\u009c\7\60\2\2\u009c\u009d"+
		"\7\26\2\2\u009d\u009e\5(\25\2\u009e!\3\2\2\2\u009f\u00a0\5\4\3\2\u00a0"+
		"\u00a1\7\27\2\2\u00a1\u00cb\3\2\2\2\u00a2\u00a3\7\n\2\2\u00a3\u00a4\5"+
		"(\25\2\u00a4\u00a5\7\13\2\2\u00a5\u00a8\5$\23\2\u00a6\u00a7\7\f\2\2\u00a7"+
		"\u00a9\5&\24\2\u00a8\u00a6\3\2\2\2\u00a8\u00a9\3\2\2\2\u00a9\u00cb\3\2"+
		"\2\2\u00aa\u00ab\7\r\2\2\u00ab\u00ac\7\"\2\2\u00ac\u00ad\7\60\2\2\u00ad"+
		"\u00ae\7\3\2\2\u00ae\u00af\5(\25\2\u00af\u00b0\7\27\2\2\u00b0\u00b1\5"+
		"(\25\2\u00b1\u00b2\7\27\2\2\u00b2\u00b3\5(\25\2\u00b3\u00b4\7#\2\2\u00b4"+
		"\u00b5\5\"\22\2\u00b5\u00cb\3\2\2\2\u00b6\u00b8\7\16\2\2\u00b7\u00b9\5"+
		"(\25\2\u00b8\u00b7\3\2\2\2\u00b8\u00b9\3\2\2\2\u00b9\u00ba\3\2\2\2\u00ba"+
		"\u00cb\7\27\2\2\u00bb\u00cb\5\32\16\2\u00bc\u00bd\5\b\5\2\u00bd\u00be"+
		"\7\27\2\2\u00be\u00cb\3\2\2\2\u00bf\u00c0\5\n\6\2\u00c0\u00c1\7\27\2\2"+
		"\u00c1\u00cb\3\2\2\2\u00c2\u00c3\5(\25\2\u00c3\u00c4\7\3\2\2\u00c4\u00c5"+
		"\5(\25\2\u00c5\u00c6\7\27\2\2\u00c6\u00cb\3\2\2\2\u00c7\u00c8\5(\25\2"+
		"\u00c8\u00c9\7\27\2\2\u00c9\u00cb\3\2\2\2\u00ca\u009f\3\2\2\2\u00ca\u00a2"+
		"\3\2\2\2\u00ca\u00aa\3\2\2\2\u00ca\u00b6\3\2\2\2\u00ca\u00bb\3\2\2\2\u00ca"+
		"\u00bc\3\2\2\2\u00ca\u00bf\3\2\2\2\u00ca\u00c2\3\2\2\2\u00ca\u00c7\3\2"+
		"\2\2\u00cb#\3\2\2\2\u00cc\u00cd\5\"\22\2\u00cd%\3\2\2\2\u00ce\u00cf\5"+
		"\"\22\2\u00cf\'\3\2\2\2\u00d0\u00d1\b\25\1\2\u00d1\u00e8\5\24\13\2\u00d2"+
		"\u00e8\5\4\3\2\u00d3\u00e8\5\6\4\2\u00d4\u00d5\7\32\2\2\u00d5\u00e8\5"+
		"(\25\17\u00d6\u00d7\7(\2\2\u00d7\u00e8\5(\25\16\u00d8\u00e8\7/\2\2\u00d9"+
		"\u00e8\7,\2\2\u00da\u00e8\7-\2\2\u00db\u00dd\7\"\2\2\u00dc\u00de\5*\26"+
		"\2\u00dd\u00dc\3\2\2\2\u00dd\u00de\3\2\2\2\u00de\u00df\3\2\2\2\u00df\u00e8"+
		"\7#\2\2\u00e0\u00e2\7$\2\2\u00e1\u00e3\5*\26\2\u00e2\u00e1\3\2\2\2\u00e2"+
		"\u00e3\3\2\2\2\u00e3\u00e4\3\2\2\2\u00e4\u00e8\7%\2\2\u00e5\u00e8\5\22"+
		"\n\2\u00e6\u00e8\7\60\2\2\u00e7\u00d0\3\2\2\2\u00e7\u00d2\3\2\2\2\u00e7"+
		"\u00d3\3\2\2\2\u00e7\u00d4\3\2\2\2\u00e7\u00d6\3\2\2\2\u00e7\u00d8\3\2"+
		"\2\2\u00e7\u00d9\3\2\2\2\u00e7\u00da\3\2\2\2\u00e7\u00db\3\2\2\2\u00e7"+
		"\u00e0\3\2\2\2\u00e7\u00e5\3\2\2\2\u00e7\u00e6\3\2\2\2\u00e8\u00fb\3\2"+
		"\2\2\u00e9\u00ea\f\r\2\2\u00ea\u00eb\7\33\2\2\u00eb\u00fa\5(\25\16\u00ec"+
		"\u00ed\f\f\2\2\u00ed\u00ee\7\31\2\2\u00ee\u00fa\5(\25\r\u00ef\u00f0\f"+
		"\13\2\2\u00f0\u00f1\7\32\2\2\u00f1\u00fa\5(\25\f\u00f2\u00f3\f\n\2\2\u00f3"+
		"\u00f4\t\3\2\2\u00f4\u00fa\5(\25\13\u00f5\u00f6\f\21\2\2\u00f6\u00fa\7"+
		"\5\2\2\u00f7\u00f8\f\20\2\2\u00f8\u00fa\7\6\2\2\u00f9\u00e9\3\2\2\2\u00f9"+
		"\u00ec\3\2\2\2\u00f9\u00ef\3\2\2\2\u00f9\u00f2\3\2\2\2\u00f9\u00f5\3\2"+
		"\2\2\u00f9\u00f7\3\2\2\2\u00fa\u00fd\3\2\2\2\u00fb\u00f9\3\2\2\2\u00fb"+
		"\u00fc\3\2\2\2\u00fc)\3\2\2\2\u00fd\u00fb\3\2\2\2\u00fe\u0103\5(\25\2"+
		"\u00ff\u0100\7\30\2\2\u0100\u0102\5(\25\2\u0101\u00ff\3\2\2\2\u0102\u0105"+
		"\3\2\2\2\u0103\u0101\3\2\2\2\u0103\u0104\3\2\2\2\u0104+\3\2\2\2\u0105"+
		"\u0103\3\2\2\2\34.\60\65=AIMSWejt}\u0082\u0088\u008f\u0098\u00a8\u00b8"+
		"\u00ca\u00dd\u00e2\u00e7\u00f9\u00fb\u0103";
	public static final ATN _ATN =
		new ATNDeserializer().deserialize(_serializedATN.toCharArray());
	static {
		_decisionToDFA = new DFA[_ATN.getNumberOfDecisions()];
		for (int i = 0; i < _ATN.getNumberOfDecisions(); i++) {
			_decisionToDFA[i] = new DFA(_ATN.getDecisionState(i), i);
		}
	}
}