using System;
using System.Text.RegularExpressions;

namespace AusgleichslisteApp.Services
{
    /// <summary>
    /// Service für die Berechnung von mathematischen Ausdrücken mit +, -, *, / und Klammern.
    /// </summary>
    public interface IExpressionCalculatorService
    {
        /// <summary>
        /// Berechnet einen mathematischen Ausdruck.
        /// </summary>
        /// <param name="expression">Der zu berechnende Ausdruck (z.B. "10 + 5 * 2" oder "(10 + 5) * 2")</param>
        /// <returns>Das Ergebnis der Berechnung als Dezimalzahl</returns>
        /// <exception cref="ArgumentException">Wenn der Ausdruck ungültig ist</exception>
        decimal Calculate(string expression);
        
        /// <summary>
        /// Versucht einen Ausdruck zu berechnen und gibt an, ob die Berechnung erfolgreich war.
        /// </summary>
        bool TryCalculate(string expression, out decimal result, out string error);
    }

    public class ExpressionCalculatorService : IExpressionCalculatorService
    {
        public decimal Calculate(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                throw new ArgumentException("Ausdruck darf nicht leer sein.", nameof(expression));

            if (TryCalculate(expression, out var result, out var error))
                return result;

            throw new ArgumentException(error, nameof(expression));
        }

        public bool TryCalculate(string expression, out decimal result, out string error)
        {
            result = 0;
            error = string.Empty;

            try
            {
                if (string.IsNullOrWhiteSpace(expression))
                {
                    error = "Ausdruck darf nicht leer sein.";
                    return false;
                }

                // Entferne Whitespace
                expression = Regex.Replace(expression, @"\s+", "");

                // Validiere erlaubte Zeichen
                if (!Regex.IsMatch(expression, @"^[0-9+\-*/()\.,]+$"))
                {
                    error = "Ungültige Zeichen. Erlaubt sind: Zahlen, +, -, *, /, (, ), . oder ,";
                    return false;
                }

                // Ersetze Komma mit Punkt (deutsche Dezimal-Notation)
                expression = expression.Replace(",", ".");

                // Berechne den Ausdruck
                result = EvaluateExpression(expression);
                return true;
            }
            catch (DivideByZeroException)
            {
                error = "Division durch Null nicht möglich.";
                return false;
            }
            catch (OverflowException)
            {
                error = "Das Ergebnis ist zu groß.";
                return false;
            }
            catch (Exception ex)
            {
                error = $"Ungültiger Ausdruck: {ex.Message}";
                return false;
            }
        }

        private decimal EvaluateExpression(string expression)
        {
            // Tokenize und Parse mit Operator-Precedenz
            var tokens = Tokenize(expression);
            var result = ParseExpression(tokens, 0, out _);
            return result;
        }

        private List<string> Tokenize(string expression)
        {
            var tokens = new List<string>();
            var current = "";

            for (int i = 0; i < expression.Length; i++)
            {
                var c = expression[i];

                if (char.IsDigit(c) || c == '.')
                {
                    current += c;
                }
                else if ("+-*/()".Contains(c))
                {
                    if (!string.IsNullOrEmpty(current))
                    {
                        tokens.Add(current);
                        current = "";
                    }
                    tokens.Add(c.ToString());
                }
            }

            if (!string.IsNullOrEmpty(current))
                tokens.Add(current);

            return tokens;
        }

        private decimal ParseExpression(List<string> tokens, int startIndex, out int endIndex)
        {
            var result = ParseTerm(tokens, startIndex, out var currentIndex);

            while (currentIndex < tokens.Count && (tokens[currentIndex] == "+" || tokens[currentIndex] == "-"))
            {
                var op = tokens[currentIndex];
                currentIndex++;
                var right = ParseTerm(tokens, currentIndex, out currentIndex);

                result = op == "+" ? result + right : result - right;
            }

            endIndex = currentIndex;
            return result;
        }

        private decimal ParseTerm(List<string> tokens, int startIndex, out int endIndex)
        {
            var result = ParseFactor(tokens, startIndex, out var currentIndex);

            while (currentIndex < tokens.Count && (tokens[currentIndex] == "*" || tokens[currentIndex] == "/"))
            {
                var op = tokens[currentIndex];
                currentIndex++;
                var right = ParseFactor(tokens, currentIndex, out currentIndex);

                if (op == "*")
                    result = result * right;
                else
                {
                    if (right == 0)
                        throw new DivideByZeroException("Division durch Null");
                    result = result / right;
                }
            }

            endIndex = currentIndex;
            return result;
        }

        private decimal ParseFactor(List<string> tokens, int startIndex, out int endIndex)
        {
            if (startIndex >= tokens.Count)
                throw new ArgumentException("Ungültiger Ausdruck - unerwartetes Ende");

            var token = tokens[startIndex];

            // Negative Zahl
            if (token == "-")
            {
                var value = ParseFactor(tokens, startIndex + 1, out endIndex);
                return -value;
            }

            // Positive Zahl (+ wird ignoriert)
            if (token == "+")
            {
                return ParseFactor(tokens, startIndex + 1, out endIndex);
            }

            // Klammer
            if (token == "(")
            {
                var result = ParseExpression(tokens, startIndex + 1, out var currentIndex);
                if (currentIndex >= tokens.Count || tokens[currentIndex] != ")")
                    throw new ArgumentException("Fehlende schließende Klammer");
                endIndex = currentIndex + 1;
                return result;
            }

            // Zahl
            if (decimal.TryParse(token, out var numValue))
            {
                endIndex = startIndex + 1;
                return numValue;
            }

            throw new ArgumentException($"Ungültiger Token: {token}");
        }
    }
}
