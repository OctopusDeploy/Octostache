using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Octostache.Templates
{
    class CalculationToken : TemplateToken
    {
        public ICalculationComponent Expression { get; }

        public CalculationToken(ICalculationComponent expression)
        {
            Expression = expression;
        }

        public override string ToString() => "#{" + Expression + "}";

        public override IEnumerable<string> GetArguments() => Expression.GetArguments();
    }

    class CalculationConstant : ICalculationComponent
    {
        public double Value { get; }

        public CalculationConstant(double value)
        {
            Value = value;
        }

        public double? Evaluate(Func<SymbolExpression, string?> resolve) => Value;

        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

        public IEnumerable<string> GetArguments()
        {
            yield break;
        }
    }

    class CalculationVariable : ICalculationComponent
    {
        public SymbolExpression Variable { get; }

        public CalculationVariable(SymbolExpression variable)
        {
            Variable = variable;
        }

        public double? Evaluate(Func<SymbolExpression, string?> resolve)
        {
            var stringValue = resolve(Variable);

            if (stringValue == null)
                return null;

            if (!double.TryParse(stringValue, out var value))
                return null;

            return value;
        }

        public override string ToString() => Variable.ToString();

        public IEnumerable<string> GetArguments() => Variable.GetArguments();
    }

    class CalculationOperation : ICalculationComponent
    {
        public ICalculationComponent Left { get; }
        public CalculationOperator Op { get; }
        public ICalculationComponent Right { get; }

        public CalculationOperation(ICalculationComponent left, CalculationOperator op, ICalculationComponent right)
        {
            Left = left;
            Op = op;
            Right = right;
        }

        string OperatorAsString =>
            Op switch
            {
                CalculationOperator.Add => "+",
                CalculationOperator.Subtract => "-",
                CalculationOperator.Multiply => "*",
                CalculationOperator.Divide => "/",
                _ => throw new ArgumentOutOfRangeException(),
            };

        public double? Evaluate(Func<SymbolExpression, string?> resolve)
        {
            var leftValue = Left.Evaluate(resolve);
            if (leftValue == null)
                return null;

            var rightValue = Right.Evaluate(resolve);
            if (rightValue == null)
                return null;

            return Op switch
            {
                CalculationOperator.Add => leftValue + rightValue,
                CalculationOperator.Subtract => leftValue - rightValue,
                CalculationOperator.Multiply => leftValue * rightValue,
                CalculationOperator.Divide => leftValue / rightValue,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        public IEnumerable<string> GetArguments() => Left.GetArguments().Concat(Right.GetArguments());

        public override string ToString() => $"{Left}{OperatorAsString}{Right}";
    }

    interface ICalculationComponent
    {
        double? Evaluate(Func<SymbolExpression, string?> resolve);
        IEnumerable<string> GetArguments();
    }

    enum CalculationOperator
    {
        Add,
        Subtract,
        Multiply,
        Divide,
    }
}
