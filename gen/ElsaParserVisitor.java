// Generated from C:/Projects/Elsa/v3/src/dsl/Elsa.Dsl/Dsl\ElsaParser.g4 by ANTLR 4.9.2
import org.antlr.v4.runtime.tree.ParseTreeVisitor;

/**
 * This interface defines a complete generic visitor for a parse tree produced
 * by {@link ElsaParser}.
 *
 * @param <T> The return type of the visit operation. Use {@link Void} for
 * operations with no return type.
 */
public interface ElsaParserVisitor<T> extends ParseTreeVisitor<T> {
	/**
	 * Visit a parse tree produced by {@link ElsaParser#program}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitProgram(ElsaParser.ProgramContext ctx);
	/**
	 * Visit a parse tree produced by {@link ElsaParser#object}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitObject(ElsaParser.ObjectContext ctx);
	/**
	 * Visit a parse tree produced by {@link ElsaParser#newObject}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitNewObject(ElsaParser.NewObjectContext ctx);
	/**
	 * Visit a parse tree produced by {@link ElsaParser#varDecl}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitVarDecl(ElsaParser.VarDeclContext ctx);
	/**
	 * Visit a parse tree produced by {@link ElsaParser#localVarDecl}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitLocalVarDecl(ElsaParser.LocalVarDeclContext ctx);
	/**
	 * Visit a parse tree produced by {@link ElsaParser#type}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitType(ElsaParser.TypeContext ctx);
	/**
	 * Visit a parse tree produced by {@link ElsaParser#expressionMarker}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitExpressionMarker(ElsaParser.ExpressionMarkerContext ctx);
	/**
	 * Visit a parse tree produced by {@link ElsaParser#expressionContent}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitExpressionContent(ElsaParser.ExpressionContentContext ctx);
	/**
	 * Visit a parse tree produced by {@link ElsaParser#methodCall}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitMethodCall(ElsaParser.MethodCallContext ctx);
	/**
	 * Visit a parse tree produced by {@link ElsaParser#funcCall}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitFuncCall(ElsaParser.FuncCallContext ctx);
	/**
	 * Visit a parse tree produced by {@link ElsaParser#args}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitArgs(ElsaParser.ArgsContext ctx);
	/**
	 * Visit a parse tree produced by {@link ElsaParser#arg}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitArg(ElsaParser.ArgContext ctx);
	/**
	 * Visit a parse tree produced by {@link ElsaParser#block}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitBlock(ElsaParser.BlockContext ctx);
	/**
	 * Visit a parse tree produced by {@link ElsaParser#objectInitializer}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitObjectInitializer(ElsaParser.ObjectInitializerContext ctx);
	/**
	 * Visit a parse tree produced by {@link ElsaParser#propertyList}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitPropertyList(ElsaParser.PropertyListContext ctx);
	/**
	 * Visit a parse tree produced by {@link ElsaParser#property}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitProperty(ElsaParser.PropertyContext ctx);
	/**
	 * Visit a parse tree produced by the {@code objectStat}
	 * labeled alternative in {@link ElsaParser#stat}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitObjectStat(ElsaParser.ObjectStatContext ctx);
	/**
	 * Visit a parse tree produced by the {@code ifStat}
	 * labeled alternative in {@link ElsaParser#stat}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitIfStat(ElsaParser.IfStatContext ctx);
	/**
	 * Visit a parse tree produced by the {@code forStat}
	 * labeled alternative in {@link ElsaParser#stat}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitForStat(ElsaParser.ForStatContext ctx);
	/**
	 * Visit a parse tree produced by the {@code returnStat}
	 * labeled alternative in {@link ElsaParser#stat}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitReturnStat(ElsaParser.ReturnStatContext ctx);
	/**
	 * Visit a parse tree produced by the {@code blockStat}
	 * labeled alternative in {@link ElsaParser#stat}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitBlockStat(ElsaParser.BlockStatContext ctx);
	/**
	 * Visit a parse tree produced by the {@code variableDeclarationStat}
	 * labeled alternative in {@link ElsaParser#stat}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitVariableDeclarationStat(ElsaParser.VariableDeclarationStatContext ctx);
	/**
	 * Visit a parse tree produced by the {@code localVariableDeclarationStat}
	 * labeled alternative in {@link ElsaParser#stat}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitLocalVariableDeclarationStat(ElsaParser.LocalVariableDeclarationStatContext ctx);
	/**
	 * Visit a parse tree produced by the {@code assignmentStat}
	 * labeled alternative in {@link ElsaParser#stat}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitAssignmentStat(ElsaParser.AssignmentStatContext ctx);
	/**
	 * Visit a parse tree produced by the {@code expressionStat}
	 * labeled alternative in {@link ElsaParser#stat}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitExpressionStat(ElsaParser.ExpressionStatContext ctx);
	/**
	 * Visit a parse tree produced by {@link ElsaParser#thenStat}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitThenStat(ElsaParser.ThenStatContext ctx);
	/**
	 * Visit a parse tree produced by {@link ElsaParser#elseStat}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitElseStat(ElsaParser.ElseStatContext ctx);
	/**
	 * Visit a parse tree produced by the {@code newObjectExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitNewObjectExpr(ElsaParser.NewObjectExprContext ctx);
	/**
	 * Visit a parse tree produced by the {@code subtractExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitSubtractExpr(ElsaParser.SubtractExprContext ctx);
	/**
	 * Visit a parse tree produced by the {@code incrementExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitIncrementExpr(ElsaParser.IncrementExprContext ctx);
	/**
	 * Visit a parse tree produced by the {@code objectExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitObjectExpr(ElsaParser.ObjectExprContext ctx);
	/**
	 * Visit a parse tree produced by the {@code stringValueExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitStringValueExpr(ElsaParser.StringValueExprContext ctx);
	/**
	 * Visit a parse tree produced by the {@code multiplyExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitMultiplyExpr(ElsaParser.MultiplyExprContext ctx);
	/**
	 * Visit a parse tree produced by the {@code parenthesesExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitParenthesesExpr(ElsaParser.ParenthesesExprContext ctx);
	/**
	 * Visit a parse tree produced by the {@code functionExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitFunctionExpr(ElsaParser.FunctionExprContext ctx);
	/**
	 * Visit a parse tree produced by the {@code decrementExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitDecrementExpr(ElsaParser.DecrementExprContext ctx);
	/**
	 * Visit a parse tree produced by the {@code negateExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitNegateExpr(ElsaParser.NegateExprContext ctx);
	/**
	 * Visit a parse tree produced by the {@code methodCallExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitMethodCallExpr(ElsaParser.MethodCallExprContext ctx);
	/**
	 * Visit a parse tree produced by the {@code variableExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitVariableExpr(ElsaParser.VariableExprContext ctx);
	/**
	 * Visit a parse tree produced by the {@code notExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitNotExpr(ElsaParser.NotExprContext ctx);
	/**
	 * Visit a parse tree produced by the {@code integerValueExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitIntegerValueExpr(ElsaParser.IntegerValueExprContext ctx);
	/**
	 * Visit a parse tree produced by the {@code addExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitAddExpr(ElsaParser.AddExprContext ctx);
	/**
	 * Visit a parse tree produced by the {@code backTickStringValueExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitBackTickStringValueExpr(ElsaParser.BackTickStringValueExprContext ctx);
	/**
	 * Visit a parse tree produced by the {@code bracketsExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitBracketsExpr(ElsaParser.BracketsExprContext ctx);
	/**
	 * Visit a parse tree produced by the {@code compareExpr}
	 * labeled alternative in {@link ElsaParser#expr}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitCompareExpr(ElsaParser.CompareExprContext ctx);
	/**
	 * Visit a parse tree produced by {@link ElsaParser#exprList}.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	T visitExprList(ElsaParser.ExprListContext ctx);
}