export type ExpressionType = string;
export type Expression = LiteralExpression | JavaScriptExpression | JsonExpression;

export interface LiteralExpression {
  type: ExpressionType;
  value: any;
}

export interface JavaScriptExpression {
  type: ExpressionType;
  value: string;
}

export interface JsonExpression {
  type: ExpressionType;
  value: string;
}
