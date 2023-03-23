export type ExpressionType = string;
export type Expression = LiteralExpression | JavaScriptExpression | ObjectExpression;

export interface LiteralExpression {
  type: ExpressionType;
  value: any;
}

export interface JavaScriptExpression {
  type: ExpressionType;
  value: string;
}

export interface ObjectExpression {
  type: ExpressionType;
  value: string;
}
