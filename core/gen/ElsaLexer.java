// Generated from C:/Projects/Elsa/v3/src/dsl/Elsa.Dsl/Dsl\ElsaLexer.g4 by ANTLR 4.9.2
import org.antlr.v4.runtime.Lexer;
import org.antlr.v4.runtime.CharStream;
import org.antlr.v4.runtime.Token;
import org.antlr.v4.runtime.TokenStream;
import org.antlr.v4.runtime.*;
import org.antlr.v4.runtime.atn.*;
import org.antlr.v4.runtime.dfa.DFA;
import org.antlr.v4.runtime.misc.*;

@SuppressWarnings({"all", "warnings", "unchecked", "unused", "cast"})
public class ElsaLexer extends Lexer {
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
	public static String[] channelNames = {
		"DEFAULT_TOKEN_CHANNEL", "HIDDEN"
	};

	public static String[] modeNames = {
		"DEFAULT_MODE"
	};

	private static String[] makeRuleNames() {
		return new String[] {
			"EQ", "GREATER", "INCREMENT", "DECREMENT", "NEW", "VARIABLE", "LET", 
			"IF", "THEN", "ELSE", "FOR", "RETURN", "VOID", "FLOAT", "INT", "STRING", 
			"OBJECT", "EXPRESSION_MARKER", "SYMBOL", "COLON", "SEMICOLON", "COMMA", 
			"PLUS", "MINUS", "STAR", "EQUALS", "NOT_EQUALS", "GREATER_EQUALS", "LESS", 
			"LESS_EQUALS", "LAMBDA", "PARENTHESES_OPEN", "PARENTHESES_CLOSE", "BRACKET_OPEN", 
			"BRACKET_CLOSE", "CURLYBRACE_OPEN", "CURLYBRACE_CLOSE", "EXCLAMATION", 
			"PIPE", "PIPE_DBL", "PERIOD", "STRING_VAL", "BACKTICKSTRING_VAL", "LINE_COMMENT", 
			"INTEGER_VAL", "ID", "WS", "LETTER", "DIGIT", "ESC"
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


	public ElsaLexer(CharStream input) {
		super(input);
		_interp = new LexerATNSimulator(this,_ATN,_decisionToDFA,_sharedContextCache);
	}

	@Override
	public String getGrammarFileName() { return "ElsaLexer.g4"; }

	@Override
	public String[] getRuleNames() { return ruleNames; }

	@Override
	public String getSerializedATN() { return _serializedATN; }

	@Override
	public String[] getChannelNames() { return channelNames; }

	@Override
	public String[] getModeNames() { return modeNames; }

	@Override
	public ATN getATN() { return _ATN; }

	public static final String _serializedATN =
		"\3\u608b\ua72a\u8133\ub9ed\u417c\u3be7\u7786\u5964\2\62\u0134\b\1\4\2"+
		"\t\2\4\3\t\3\4\4\t\4\4\5\t\5\4\6\t\6\4\7\t\7\4\b\t\b\4\t\t\t\4\n\t\n\4"+
		"\13\t\13\4\f\t\f\4\r\t\r\4\16\t\16\4\17\t\17\4\20\t\20\4\21\t\21\4\22"+
		"\t\22\4\23\t\23\4\24\t\24\4\25\t\25\4\26\t\26\4\27\t\27\4\30\t\30\4\31"+
		"\t\31\4\32\t\32\4\33\t\33\4\34\t\34\4\35\t\35\4\36\t\36\4\37\t\37\4 \t"+
		" \4!\t!\4\"\t\"\4#\t#\4$\t$\4%\t%\4&\t&\4\'\t\'\4(\t(\4)\t)\4*\t*\4+\t"+
		"+\4,\t,\4-\t-\4.\t.\4/\t/\4\60\t\60\4\61\t\61\4\62\t\62\4\63\t\63\3\2"+
		"\3\2\3\3\3\3\3\4\3\4\3\4\3\5\3\5\3\5\3\6\3\6\3\6\3\6\3\7\3\7\3\7\3\7\3"+
		"\7\3\7\3\7\3\7\3\7\3\b\3\b\3\b\3\b\3\t\3\t\3\t\3\n\3\n\3\n\3\n\3\n\3\13"+
		"\3\13\3\13\3\13\3\13\3\f\3\f\3\f\3\f\3\r\3\r\3\r\3\r\3\r\3\r\3\r\3\16"+
		"\3\16\3\16\3\16\3\16\3\17\3\17\3\17\3\17\3\17\3\17\3\20\3\20\3\20\3\20"+
		"\3\21\3\21\3\21\3\21\3\21\3\21\3\21\3\22\3\22\3\22\3\22\3\22\3\22\3\22"+
		"\3\23\3\23\3\23\3\23\3\23\3\23\3\23\3\23\3\23\3\23\3\23\3\24\3\24\3\25"+
		"\3\25\3\26\3\26\3\27\3\27\3\30\3\30\3\31\3\31\3\32\3\32\3\33\3\33\3\33"+
		"\3\34\3\34\3\34\3\35\3\35\3\35\3\36\3\36\3\37\3\37\3\37\3 \3 \3 \3!\3"+
		"!\3\"\3\"\3#\3#\3$\3$\3%\3%\3&\3&\3\'\3\'\3(\3(\3)\3)\3)\3*\3*\3+\3+\3"+
		"+\3+\7+\u00fb\n+\f+\16+\u00fe\13+\3+\3+\3,\3,\3,\3,\7,\u0106\n,\f,\16"+
		",\u0109\13,\3,\3,\3-\3-\3-\3-\7-\u0111\n-\f-\16-\u0114\13-\3-\5-\u0117"+
		"\n-\3-\3-\3-\3-\3.\6.\u011e\n.\r.\16.\u011f\3/\3/\3/\7/\u0125\n/\f/\16"+
		"/\u0128\13/\3\60\3\60\3\60\3\60\3\61\3\61\3\62\3\62\3\63\3\63\3\63\5\u00fc"+
		"\u0107\u0112\2\64\3\3\5\4\7\5\t\6\13\7\r\b\17\t\21\n\23\13\25\f\27\r\31"+
		"\16\33\17\35\20\37\21!\22#\23%\24\'\25)\26+\27-\30/\31\61\32\63\33\65"+
		"\34\67\359\36;\37= ?!A\"C#E$G%I&K\'M(O)Q*S+U,W-Y.[/]\60_\61a\2c\2e\62"+
		"\3\2\6\6\2&&--?@bb\5\2\13\f\17\17\"\"\5\2C\\aac|\3\2\62;\2\u013a\2\3\3"+
		"\2\2\2\2\5\3\2\2\2\2\7\3\2\2\2\2\t\3\2\2\2\2\13\3\2\2\2\2\r\3\2\2\2\2"+
		"\17\3\2\2\2\2\21\3\2\2\2\2\23\3\2\2\2\2\25\3\2\2\2\2\27\3\2\2\2\2\31\3"+
		"\2\2\2\2\33\3\2\2\2\2\35\3\2\2\2\2\37\3\2\2\2\2!\3\2\2\2\2#\3\2\2\2\2"+
		"%\3\2\2\2\2\'\3\2\2\2\2)\3\2\2\2\2+\3\2\2\2\2-\3\2\2\2\2/\3\2\2\2\2\61"+
		"\3\2\2\2\2\63\3\2\2\2\2\65\3\2\2\2\2\67\3\2\2\2\29\3\2\2\2\2;\3\2\2\2"+
		"\2=\3\2\2\2\2?\3\2\2\2\2A\3\2\2\2\2C\3\2\2\2\2E\3\2\2\2\2G\3\2\2\2\2I"+
		"\3\2\2\2\2K\3\2\2\2\2M\3\2\2\2\2O\3\2\2\2\2Q\3\2\2\2\2S\3\2\2\2\2U\3\2"+
		"\2\2\2W\3\2\2\2\2Y\3\2\2\2\2[\3\2\2\2\2]\3\2\2\2\2_\3\2\2\2\2e\3\2\2\2"+
		"\3g\3\2\2\2\5i\3\2\2\2\7k\3\2\2\2\tn\3\2\2\2\13q\3\2\2\2\ru\3\2\2\2\17"+
		"~\3\2\2\2\21\u0082\3\2\2\2\23\u0085\3\2\2\2\25\u008a\3\2\2\2\27\u008f"+
		"\3\2\2\2\31\u0093\3\2\2\2\33\u009a\3\2\2\2\35\u009f\3\2\2\2\37\u00a5\3"+
		"\2\2\2!\u00a9\3\2\2\2#\u00b0\3\2\2\2%\u00b7\3\2\2\2\'\u00c2\3\2\2\2)\u00c4"+
		"\3\2\2\2+\u00c6\3\2\2\2-\u00c8\3\2\2\2/\u00ca\3\2\2\2\61\u00cc\3\2\2\2"+
		"\63\u00ce\3\2\2\2\65\u00d0\3\2\2\2\67\u00d3\3\2\2\29\u00d6\3\2\2\2;\u00d9"+
		"\3\2\2\2=\u00db\3\2\2\2?\u00de\3\2\2\2A\u00e1\3\2\2\2C\u00e3\3\2\2\2E"+
		"\u00e5\3\2\2\2G\u00e7\3\2\2\2I\u00e9\3\2\2\2K\u00eb\3\2\2\2M\u00ed\3\2"+
		"\2\2O\u00ef\3\2\2\2Q\u00f1\3\2\2\2S\u00f4\3\2\2\2U\u00f6\3\2\2\2W\u0101"+
		"\3\2\2\2Y\u010c\3\2\2\2[\u011d\3\2\2\2]\u0121\3\2\2\2_\u0129\3\2\2\2a"+
		"\u012d\3\2\2\2c\u012f\3\2\2\2e\u0131\3\2\2\2gh\7?\2\2h\4\3\2\2\2ij\7@"+
		"\2\2j\6\3\2\2\2kl\7-\2\2lm\7-\2\2m\b\3\2\2\2no\7/\2\2op\7/\2\2p\n\3\2"+
		"\2\2qr\7p\2\2rs\7g\2\2st\7y\2\2t\f\3\2\2\2uv\7x\2\2vw\7c\2\2wx\7t\2\2"+
		"xy\7k\2\2yz\7c\2\2z{\7d\2\2{|\7n\2\2|}\7g\2\2}\16\3\2\2\2~\177\7n\2\2"+
		"\177\u0080\7g\2\2\u0080\u0081\7v\2\2\u0081\20\3\2\2\2\u0082\u0083\7k\2"+
		"\2\u0083\u0084\7h\2\2\u0084\22\3\2\2\2\u0085\u0086\7v\2\2\u0086\u0087"+
		"\7j\2\2\u0087\u0088\7g\2\2\u0088\u0089\7p\2\2\u0089\24\3\2\2\2\u008a\u008b"+
		"\7g\2\2\u008b\u008c\7n\2\2\u008c\u008d\7u\2\2\u008d\u008e\7g\2\2\u008e"+
		"\26\3\2\2\2\u008f\u0090\7h\2\2\u0090\u0091\7q\2\2\u0091\u0092\7t\2\2\u0092"+
		"\30\3\2\2\2\u0093\u0094\7t\2\2\u0094\u0095\7g\2\2\u0095\u0096\7v\2\2\u0096"+
		"\u0097\7w\2\2\u0097\u0098\7t\2\2\u0098\u0099\7p\2\2\u0099\32\3\2\2\2\u009a"+
		"\u009b\7x\2\2\u009b\u009c\7q\2\2\u009c\u009d\7k\2\2\u009d\u009e\7f\2\2"+
		"\u009e\34\3\2\2\2\u009f\u00a0\7h\2\2\u00a0\u00a1\7n\2\2\u00a1\u00a2\7"+
		"q\2\2\u00a2\u00a3\7c\2\2\u00a3\u00a4\7v\2\2\u00a4\36\3\2\2\2\u00a5\u00a6"+
		"\7k\2\2\u00a6\u00a7\7p\2\2\u00a7\u00a8\7v\2\2\u00a8 \3\2\2\2\u00a9\u00aa"+
		"\7u\2\2\u00aa\u00ab\7v\2\2\u00ab\u00ac\7t\2\2\u00ac\u00ad\7k\2\2\u00ad"+
		"\u00ae\7p\2\2\u00ae\u00af\7i\2\2\u00af\"\3\2\2\2\u00b0\u00b1\7q\2\2\u00b1"+
		"\u00b2\7d\2\2\u00b2\u00b3\7l\2\2\u00b3\u00b4\7g\2\2\u00b4\u00b5\7e\2\2"+
		"\u00b5\u00b6\7v\2\2\u00b6$\3\2\2\2\u00b7\u00b8\7g\2\2\u00b8\u00b9\7z\2"+
		"\2\u00b9\u00ba\7r\2\2\u00ba\u00bb\7t\2\2\u00bb\u00bc\7g\2\2\u00bc\u00bd"+
		"\7u\2\2\u00bd\u00be\7u\2\2\u00be\u00bf\7k\2\2\u00bf\u00c0\7q\2\2\u00c0"+
		"\u00c1\7p\2\2\u00c1&\3\2\2\2\u00c2\u00c3\t\2\2\2\u00c3(\3\2\2\2\u00c4"+
		"\u00c5\7<\2\2\u00c5*\3\2\2\2\u00c6\u00c7\7=\2\2\u00c7,\3\2\2\2\u00c8\u00c9"+
		"\7.\2\2\u00c9.\3\2\2\2\u00ca\u00cb\7-\2\2\u00cb\60\3\2\2\2\u00cc\u00cd"+
		"\7/\2\2\u00cd\62\3\2\2\2\u00ce\u00cf\7,\2\2\u00cf\64\3\2\2\2\u00d0\u00d1"+
		"\7?\2\2\u00d1\u00d2\7?\2\2\u00d2\66\3\2\2\2\u00d3\u00d4\7#\2\2\u00d4\u00d5"+
		"\7?\2\2\u00d58\3\2\2\2\u00d6\u00d7\7@\2\2\u00d7\u00d8\7?\2\2\u00d8:\3"+
		"\2\2\2\u00d9\u00da\7>\2\2\u00da<\3\2\2\2\u00db\u00dc\7>\2\2\u00dc\u00dd"+
		"\7?\2\2\u00dd>\3\2\2\2\u00de\u00df\7?\2\2\u00df\u00e0\7@\2\2\u00e0@\3"+
		"\2\2\2\u00e1\u00e2\7*\2\2\u00e2B\3\2\2\2\u00e3\u00e4\7+\2\2\u00e4D\3\2"+
		"\2\2\u00e5\u00e6\7]\2\2\u00e6F\3\2\2\2\u00e7\u00e8\7_\2\2\u00e8H\3\2\2"+
		"\2\u00e9\u00ea\7}\2\2\u00eaJ\3\2\2\2\u00eb\u00ec\7\177\2\2\u00ecL\3\2"+
		"\2\2\u00ed\u00ee\7#\2\2\u00eeN\3\2\2\2\u00ef\u00f0\7~\2\2\u00f0P\3\2\2"+
		"\2\u00f1\u00f2\7~\2\2\u00f2\u00f3\7~\2\2\u00f3R\3\2\2\2\u00f4\u00f5\7"+
		"\60\2\2\u00f5T\3\2\2\2\u00f6\u00fc\7$\2\2\u00f7\u00f8\7^\2\2\u00f8\u00fb"+
		"\7$\2\2\u00f9\u00fb\13\2\2\2\u00fa\u00f7\3\2\2\2\u00fa\u00f9\3\2\2\2\u00fb"+
		"\u00fe\3\2\2\2\u00fc\u00fd\3\2\2\2\u00fc\u00fa\3\2\2\2\u00fd\u00ff\3\2"+
		"\2\2\u00fe\u00fc\3\2\2\2\u00ff\u0100\7$\2\2\u0100V\3\2\2\2\u0101\u0107"+
		"\7b\2\2\u0102\u0103\7^\2\2\u0103\u0106\7$\2\2\u0104\u0106\13\2\2\2\u0105"+
		"\u0102\3\2\2\2\u0105\u0104\3\2\2\2\u0106\u0109\3\2\2\2\u0107\u0108\3\2"+
		"\2\2\u0107\u0105\3\2\2\2\u0108\u010a\3\2\2\2\u0109\u0107\3\2\2\2\u010a"+
		"\u010b\7b\2\2\u010bX\3\2\2\2\u010c\u010d\7\61\2\2\u010d\u010e\7\61\2\2"+
		"\u010e\u0112\3\2\2\2\u010f\u0111\13\2\2\2\u0110\u010f\3\2\2\2\u0111\u0114"+
		"\3\2\2\2\u0112\u0113\3\2\2\2\u0112\u0110\3\2\2\2\u0113\u0116\3\2\2\2\u0114"+
		"\u0112\3\2\2\2\u0115\u0117\7\17\2\2\u0116\u0115\3\2\2\2\u0116\u0117\3"+
		"\2\2\2\u0117\u0118\3\2\2\2\u0118\u0119\7\f\2\2\u0119\u011a\3\2\2\2\u011a"+
		"\u011b\b-\2\2\u011bZ\3\2\2\2\u011c\u011e\5c\62\2\u011d\u011c\3\2\2\2\u011e"+
		"\u011f\3\2\2\2\u011f\u011d\3\2\2\2\u011f\u0120\3\2\2\2\u0120\\\3\2\2\2"+
		"\u0121\u0126\5a\61\2\u0122\u0125\5a\61\2\u0123\u0125\5c\62\2\u0124\u0122"+
		"\3\2\2\2\u0124\u0123\3\2\2\2\u0125\u0128\3\2\2\2\u0126\u0124\3\2\2\2\u0126"+
		"\u0127\3\2\2\2\u0127^\3\2\2\2\u0128\u0126\3\2\2\2\u0129\u012a\t\3\2\2"+
		"\u012a\u012b\3\2\2\2\u012b\u012c\b\60\2\2\u012c`\3\2\2\2\u012d\u012e\t"+
		"\4\2\2\u012eb\3\2\2\2\u012f\u0130\t\5\2\2\u0130d\3\2\2\2\u0131\u0132\7"+
		"^\2\2\u0132\u0133\7~\2\2\u0133f\3\2\2\2\f\2\u00fa\u00fc\u0105\u0107\u0112"+
		"\u0116\u011f\u0124\u0126\3\b\2\2";
	public static final ATN _ATN =
		new ATNDeserializer().deserialize(_serializedATN.toCharArray());
	static {
		_decisionToDFA = new DFA[_ATN.getNumberOfDecisions()];
		for (int i = 0; i < _ATN.getNumberOfDecisions(); i++) {
			_decisionToDFA[i] = new DFA(_ATN.getDecisionState(i), i);
		}
	}
}