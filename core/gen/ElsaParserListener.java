// Generated from C:/Projects/Elsa/v3/src/dsl/Elsa.Dsl/Dsl\ElsaParser.g4 by ANTLR 4.9.2
import org.antlr.v4.runtime.tree.ParseTreeListener;

/**
 * This interface defines a complete listener for a parse tree produced by
 * {@link ElsaParser}.
 */
public interface ElsaParserListener extends ParseTreeListener {
	/**
	 * Enter a parse tree produced by {@link ElsaParser#program}.
	 * @param ctx the parse tree
	 */
	void enterProgram(ElsaParser.ProgramContext ctx);
	/**
	 * Exit a parse tree produced by {@link ElsaParser#program}.
	 * @param ctx the parse tree
	 */
	void exitProgram(ElsaParser.ProgramContext ctx);
	/**
	 * Enter a parse tree produced by {@link ElsaParser#object}.
	 * @param ctx the parse tree
	 */
	void enterObject(ElsaParser.ObjectContext ctx);
	/**
	 * Exit a parse tree produced by {@link ElsaParser#object}.
	 * @param ctx the parse tree
	 */
	void exitObject(ElsaParser.ObjectContext ctx);
	/**
	 * Enter a parse tree produced by {@link ElsaParser#newObject}.
	 * @param ctx the parse tree
	 */
	void enterNewObject(ElsaParser.NewObjectContext ctx);
	/**
	 * Exit a parse tree produced by {@link ElsaParser#newObject}.
	 * @param ctx the parse tree
	 */
	void exitNewObject(ElsaParser.NewObjectContext ctx);
	/**
	 * Enter a parse tree produced by {@link ElsaParser#varDecl}.
	 * @param ctx the parse tree
	 */
	void enterVarDecl(ElsaParser.VarDeclContext ctx);
	/**
	 * Exit a parse tree produced by {@link ElsaParser#varDecl}.
	 * @param ctx the parse tree
	 */
	void exitVarDecl(ElsaParser.VarDeclContext ctx);
	/**
	 * Enter a parse tree produced by {@link ElsaParser#localVarDecl}.
	 * @param ctx the parse tree
	 */
	void enterLocalVarDecl(ElsaParser.LocalVarDeclContext ctx);
	/**
	 * Exit a parse tree produced by {@link ElsaParser#localVarDecl}.
	 * @param ctx the parse tree
	 */
	void exitLocalVarDecl(ElsaParser.LocalVarDeclContext ctx);
	/**
	 * Enter a parse tree produced by {@link ElsaParser#type}.
	 * @param ctx the parse tree
	 */
	void enterType(ElsaParser.TypeContext ctx);
	/**
	 * Exit a parse tree produced by {@link ElsaParser#type}.
	 * @param ctx the parse tree
	 */
	void exitType(ElsaParser.TypeContext ctx);
	/**
	 * Enter a parse tree produced by {@link ElsaParser#expressionMarker}.
	 * @param ctx the parse tree
	 */
	void enterExpressionMarker(ElsaParser.ExpressionMarkerContext ctx);
	/**
	 * Exit a parse tree produced by {@link ElsaParser#expressionMarker}.
	 * @param ctx the parse tree
	 */
	void exitExpressionMarker(ElsaParser.ExpressionMarkerContext ctx);
	/**
	 * Enter a parse tree produced by {@link ElsaParser#expressionContent}.
	 * @param ctx the parse tree
	 */
	void enterExpressionContent(ElsaParser.ExpressionContentContext ctx);
	/**
	 * Exit a parse tree produced by {@link ElsaParser#expressionContent}.
	 * @param ctx the parse tree
	 */
	void exitExpressionContent(ElsaParser.ExpressionContentContext ctx);
	/**
	 * Enter a parse tree produced by {@link ElsaParser#methodCall}.
	 * @param ctx the parse tree
	 */
	void enterMethodCall(ElsaParser.MethodCallContext ctx);
	/**
	 * Exit a parse tree produced by {@link ElsaParser#methodCall}.
	 * @param ctx the parse tree
	 */
	void exitMethodCall(ElsaParser.MethodCallContext ctx);
	/**
	 * Enter a parse tree produced by {@link ElsaParser#funcCall}.
	 * @param ctx the parse tree
	 */
	void enterFuncCall(ElsaParser.FuncCallContext ctx);
	/**
	 * Exit a parse tree produced by {@link ElsaParser#funcCall}.
	 * @param ctx the parse tree
	 */
	void exitFuncCall(ElsaParser.FuncCallContext ctx);
	/**
	 * Enter a parse tree produced by {@link ElsaParser#args}.
	 * @param ctx the parse tree
	 */
	void enterArgs(ElsaParser.ArgsContext ctx);
	/**
	 * Exit a parse tree produced by {@link ElsaParser#args}.
	 * @param ctx the parse tree
	 */
	void exitArgs(ElsaParser.ArgsContext ctx);
	/**
	 * Enter a parse tree produced by {@link ElsaParser#arg}.
	 * @param ctx the parse tree
	 */
	void enterArg(ElsaParser.ArgContext ctx);
	/**
	 * Exit a parse tree produced by {@link ElsaParser#arg}.
	 * @param ctx the parse tree
	 */
	void exitArg(ElsaParser.ArgContext ctx);
	/**
	 * Enter a parse tree produced by {@link ElsaParser#block}.
	 * @param ctx the parse tree
	 */
	void enterBlock(ElsaParser.BlockContext ctx);
	/**
	 * Exit a parse tree produced by {@link ElsaParser#block}.
	 * @param ctx the parse tree
	 */
	void exitBlock(ElsaParser.BlockContext ctx);
	/**
	 * Enter a parse tree produced by {@link ElsaParser#objectInitializer}.
	 * @param ctx the parse tree
	 */
	void enterObjectInitializer(ElsaParser.ObjectInitializerContext ctx);
	/**
	 * Exit a parse tree produced by {@link ElsaParser#objectInitializer}.
	 * @param ctx the parse tree
	 */
	void exitObjectInitializer(ElsaParser.ObjectInitializerContext ctx);
	/**
	 * Enter a parse tree produced by {@link ElsaParser#propertyList}.
	 * @param ctx the parse tree
	 */
	void enterPropertyList(ElsaParser.PropertyListContext ctx);
	/**
	 * Exit a parse tree produced by {@link ElsaParser#propertyList}.
	 * @param ctx the parse tree
	 */
	void exitPropertyList(ElsaParser.PropertyListContext ctx);
	/**
	 * Enter a parse tree produced by {@link ElsaParser#property}.
	 * @param ctx the parse tree
	 */
	void enterProperty(ElsaParser.PropertyContext ctx);
	/**
	 * Exit a parse tree produced by {@link ElsaParser#property}.
	 * @param ctx the parse tree
	 */
	void exitProperty(ElsaParser.PropertyContext ctx);
	/**
	 * Enter a parse tree produced by the {@code objectStat}
	 * labeled alternative in {@link ElsaParser#stat}.
	 * @param ctx the parse tree
	 */
	void enterObjectStat(ElsaParser.ObjectStatContext ctx);
	/**
	 * Exit a parse tree produced by the {@code objectStat}
	 * labeled alternative in {@link ElsaParser#stat}.
	 * @param ctx the parse tree
	 */
	void exitObjectStat(ElsaParser.ObjectStatContext ctx);
	/**
	 * Enter a parse tree produced by the {@code ifStat}
	 * labeled alternative in {@link ElsaParser#stat}.
	 * @param ctx the parse tree
	 */
	void enterIfStat(ElsaParser.IfStatContext ctx);
	/**
	 * Exit a parse tree produced by the {@code ifStat}
	 * labeled alternative in {@link ElsaParser#stat}.
	 * @param ctx the parse tree
	 */
	void exitIfStat(ElsaParser.IfStatContext ctx);
	/**
	 * Enter a parse tree produced by the {@code forStat}
	 * labeled alternative in {@link ElsaParser#stat}.
	 * @param ctx the parse tree
	 */
	void enterForStat(ElsaParser.ForStatContext ctx);
	/**
	 * Exit a parse tree produced by the {@code forStat}
	 * labeled alternative in {@link ElsaParser#stat}.
	 * @param ctx the parse tree
	 */
	void exitForStat(ElsaParser.ForStatContext ctx);
	/**
	 * Enter a parse tree produced by the {@code returnStat}
	 * labeled alternative in {@link ElsaParser#stat}.
	 * @param ctx the parse tree
	 */
	void enterReturnStat(ElsaParser.ReturnStatContext ctx);
	/**
	 * Exit a parse tree produced by the {@code returnStat}
	 * labeled alternative in {@link ElsaParser#stat}.
	 * @param ctx the parse tree
	 */
	void exitReturnStat(ElsaParser.ReturnStatContext ctx);
	/**
	 * Enter a parse tree produced by the {@code blockStat}
	 * labeled alternative in {@link ElsaParser#stat}.
	 * @param ctx the parse tree
	 */
	void enterBlockStat(ElsaParser.BlockStatContext ctx);
	/**
	 * Exit a parse tree produced by the {@code blockStat}
	 * labeled alternative in {@link ElsaParser#stat}.
	 * @param ctx the parse tree
	 */
	void exitBlockStat(ElsaParser.BlockStatContext ctx);
	/**
	 * Enter a parse tree produced by the {@code variableDeclarationStat}
	 * labeled alternative in {@link ElsaParser#stat}.
	 * @param ctx the parse tree
	 */
	void enterVariableDeclarationStat(ElsaParser.VariableDeclarationStatContext ctx);
	/**
	 * Exit a parse tree produced by the {@code variableDeclarationStat}
	 * labeled alternative in {@link ElsaParser#stat}.
	 * @param ctx the parse tree
	 */
	void exitVariableDeclarationStat(ElsaParser.VariableDeclarationStatContext ctx);
	/**
	 * Enter a parse tree produced by the {@code localVariableDeclarationStat}
	 * labeled alternative in {@link ElsaParser#stat}.
	 * @param ctx the parse tree
	 */
	void enterLocalVariableDeclarationStat(ElsaParser.LocalVariableDeclarationStatContext ctx);
	/**
	 * Exit a parse tree produced by the {@code localVariableDeclarationStat}
	 * labeled alternative in {@link ElsaParser#stat}.
	 * @param ctx the parse tree
	 */
	void exitLocalVariableDeclarationStat(ElsaParser.LocalVariableDeclarationStatContext ctx);
	/**
	 * Enter a parse tree produced by the {@code assignmentStat}
	 * labeled alternative in {@link ElsaParser#stat}.
	 * @param ctx the parse tree
	 */
	void enterAssignmentStat(ElsaParser.AssignmentStatContext ctx);
	/**
	 * Exit a parse tree produced by the {@code assignmentStat}
	 * labeled alternative in {@link ElsaParser#stat}.
	 * @param ctx the parse tree
	 */
	void exitAssignmentStat(ElsaParser.AssignmentStatContext ctx);
	/**
	 * Enter a parse tree produced by the {@code expressionStat}
	 * labeled alternative in {@link ElsaParser#stat}.
	 * @param ctx the parse tree
	 */
	void enterExpressionStat(ElsaParser.ExpressionStatContext ctx);
	/**
	 * Exit a parse tree produced by the {@code expressionStat}
	 * labeled alternative in {@link ElsaParser#stat}.
	 * @param ctx the parse tree
	 */
	void exitExpressionStat(ElsaParser.ExpressionStatContext ctx);
	/**
	 * Enter a parse tree produced by {@link ElsaParser#thenStat}.
	 * @param ctx the parse tree
	 */
	void enterThenStat(ElsaParser.ThenStatContext ctx);
	/**
	 * Exit a parse tree produced by {@link ElsaParser#thenStat}.
	 * @param ctx the parse tree
	 */
	void exitThenStat(ElsaParser.ThenStatContext ctx);
	/**
	 * Enter a parse tree produced by {@link ElsaParser#elseStat}.
	 * @param ctx the parse tree
	 */
	void enterElseStat(ElsaParser.ElseStatContext ctx);
	/**
	 * Exit a parse tree produced by {@link ElsaParser#elseStat}.
	 * @param ctx the parse tree
	 */
	void exitElseStat(ElsaParser.ElseStatContext ctx);
	/**
	 * Enter a parse tree produced by the {@code newObjectExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void enterNewObjectExpr(ElsaParser.NewObjectExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code newObjectExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void exitNewObjectExpr(ElsaParser.NewObjectExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code subtractExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void enterSubtractExpr(ElsaParser.SubtractExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code subtractExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void exitSubtractExpr(ElsaParser.SubtractExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code incrementExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void enterIncrementExpr(ElsaParser.IncrementExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code incrementExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void exitIncrementExpr(ElsaParser.IncrementExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code objectExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void enterObjectExpr(ElsaParser.ObjectExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code objectExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void exitObjectExpr(ElsaParser.ObjectExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code stringValueExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void enterStringValueExpr(ElsaParser.StringValueExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code stringValueExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void exitStringValueExpr(ElsaParser.StringValueExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code multiplyExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void enterMultiplyExpr(ElsaParser.MultiplyExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code multiplyExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void exitMultiplyExpr(ElsaParser.MultiplyExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code parenthesesExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void enterParenthesesExpr(ElsaParser.ParenthesesExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code parenthesesExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void exitParenthesesExpr(ElsaParser.ParenthesesExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code functionExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void enterFunctionExpr(ElsaParser.FunctionExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code functionExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void exitFunctionExpr(ElsaParser.FunctionExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code decrementExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void enterDecrementExpr(ElsaParser.DecrementExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code decrementExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void exitDecrementExpr(ElsaParser.DecrementExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code negateExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void enterNegateExpr(ElsaParser.NegateExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code negateExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void exitNegateExpr(ElsaParser.NegateExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code methodCallExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void enterMethodCallExpr(ElsaParser.MethodCallExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code methodCallExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void exitMethodCallExpr(ElsaParser.MethodCallExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code variableExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void enterVariableExpr(ElsaParser.VariableExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code variableExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void exitVariableExpr(ElsaParser.VariableExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code notExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void enterNotExpr(ElsaParser.NotExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code notExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void exitNotExpr(ElsaParser.NotExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code integerValueExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void enterIntegerValueExpr(ElsaParser.IntegerValueExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code integerValueExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void exitIntegerValueExpr(ElsaParser.IntegerValueExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code addExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void enterAddExpr(ElsaParser.AddExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code addExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void exitAddExpr(ElsaParser.AddExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code backTickStringValueExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void enterBackTickStringValueExpr(ElsaParser.BackTickStringValueExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code backTickStringValueExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void exitBackTickStringValueExpr(ElsaParser.BackTickStringValueExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code bracketsExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void enterBracketsExpr(ElsaParser.BracketsExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code bracketsExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void exitBracketsExpr(ElsaParser.BracketsExprContext ctx);
	/**
	 * Enter a parse tree produced by the {@code compareExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void enterCompareExpr(ElsaParser.CompareExprContext ctx);
	/**
	 * Exit a parse tree produced by the {@code compareExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 */
	void exitCompareExpr(ElsaParser.CompareExprContext ctx);
	/**
	 * Enter a parse tree produced by {@link ElsaParser#exprList}.
	 * @param ctx the parse tree
	 */
	void enterExprList(ElsaParser.ExprListContext ctx);
	/**
	 * Exit a parse tree produced by {@link ElsaParser#exprList}.
	 * @param ctx the parse tree
	 */
	void exitExprList(ElsaParser.ExprListContext ctx);
}