using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicTable
{
    public static partial class Parsing
    {
        #region Public Methods

        /// <summary>
        /// This method split the equation (string) into a tree of equations (string) according the parentheses
        /// </summary>
        /// <param name="eq">equation</param>
        /// <param name="index">starting index of the equation in the string</param>
        /// <returns>tree</returns>
        public static UnparsedEq DivideStr(string eq, ref int index)
        {
            UnparsedEq result = new UnparsedEq();
            result.str = "";
            result.subEqs = new List<UnparsedEq>();
            int nbSubEq = 0;
            while (eq[index] != ')')
            {
                char current = eq[index];
                if (current == '(')
                {
                    index++;
                    //We generate an unique id for each subtree to link it in the main string
                    result.str += "%" + nbSubEq.ToString("000");
                    nbSubEq++;
                    result.subEqs.Add(DivideStr(eq, ref index));
                }
                else
                    result.str += current;
                index++;
            }
            return result;
        }

        /// <summary>
        /// Generates html code containing the logic table of the equation
        /// </summary>
        /// <param name="eq">equation</param>
        /// <returns>html</returns>
        public static string GenerateHTML(string eq)
        {
            //needed for things
            eq += ')';
            //for a better search
            eq = eq.Replace("->", "→");
            eq = eq.Replace("<→", "*");
            var result = new StringBuilder();
            result.Append(
@"
<!DOCTYPE HTML>
<html>
<head>
    <meta charset=""UTF-8"" />
    <style>
table,
th,
td {
    border: 1px solid black;
    padding: 5px;
}

table {
    border-collapse: collapse;
}
.green {
    color : white;
    background-color : green;
}
.red {
    color : white;
    background-color : red;
}
    </style>
    <title>Table logique</title>" +
@"
</head>
<body>
    <table>
        <tbody>");
            string footer =
@"
        </tbody>
    </table>
</body>
</html>
";
            //the index of the equation
            int index = 0;
            //all the variables found in the eqution
            var registeredVars = new List<string>();
            //main equation
            Equation globalEq;
            try
            {
                //first we split the main equation into trees
                var unparsed = DivideStr(eq, ref index);
                //then we parse it
                globalEq = Parse(unparsed.str, unparsed, registeredVars);
            }
            catch (Exception)
            {
                return null;
            }
            //we sort it alphabetically
            registeredVars.Sort();
            //We generate each possible case of combinaisons from the given variables
            var cases = new List<Dictionary<string, bool>>();
            for (int i = 0; i < Math.Pow(2, registeredVars.Count); i++)
            {
                //each case contain an unique set of states for the variables
                var currCase = new Dictionary<string, bool>();
                //magic binary stuff happen here
                for (int j = registeredVars.Count - 1; j >= 0; j--)
                    currCase.Add(registeredVars[registeredVars.Count - j - 1], (1 << j & i) == 0);
                cases.Add(currCase);
            }
            result.Append(
@"
            <tr>
                <th></th>");
            //we generate a column for each var
            foreach (var variable in registeredVars)
                result.Append(
@"
                <th>" + variable + "</th>");
            var signature = getStr(globalEq);
            result.Append(
@"
                <th><em>" + signature +
@"</em></th>
            </tr>");
            //and we generate the html for each possible case
            for (int i = 0; i < cases.Count; i++)
            {
                var key = cases[i];
                var value = globalEq.Test(key);
                result.Append(
@"
            <tr>
                <td><em>i" + i + "</em></td>");
                foreach (var variable in key)
                {
                    result.Append(
@"
                <td class=""" + (variable.Value ? "green" : "red") + "\">" + (variable.Value ? "VRAI" : "FAUX") + "</td>");
                }
                result.Append(
@"
                <td class=""" + (value ? "green" : "red") + "\">" + (value ? "VRAI" : "FAUX") + "</td>");
                result.Append(
@"
            </tr>");
            }
            result.Append(footer);
            return result.ToString();
        }

        /// <summary>
        /// returns a visual variant of an equation
        /// </summary>
        /// <param name="eq"></param>
        /// <returns></returns>
        static public string getStr(Equation eq)
        {
            if (eq is Not neq)
                return "!" + getStr(neq.InternalEquation);
            if (eq is Constant c)
                return c.Name;
            if (eq is OperatorEquation oeq)
            {
                switch (oeq.Operator)
                {
                    case OperatorEquation.OperatorType.AND:
                        return '(' + getStr(oeq.Left) + " & " + getStr(oeq.Right) + ')';

                    case OperatorEquation.OperatorType.OR:
                        return '(' + getStr(oeq.Left) + " | " + getStr(oeq.Right) + ')';

                    case OperatorEquation.OperatorType.EQUIVALENT:
                        return '(' + getStr(oeq.Left) + " 🡘 " + getStr(oeq.Right) + ')';

                    case OperatorEquation.OperatorType.INVOLVING:
                        return '(' + getStr(oeq.Left) + " → " + getStr(oeq.Right) + ')';
                }
            }
            return "";
        }

        /// <summary>
        /// Returns an equation from the given string, and other stuff
        /// </summary>
        /// <param name="str">equation to parse</param>
        /// <param name="currentUnparsed">current tree</param>
        /// <param name="registeredVars">the variables</param>
        /// <returns>output equation</returns>
        public static Equation Parse(string str, UnparsedEq currentUnparsed, List<string> registeredVars)
        {
            //this is a recursive function that parse every level of a tree at a time
            //we operate everything according to the priority order from lower to higher priority
            int index = str.IndexOf("*");//<->
            if (index == -1)
            {
                index = str.IndexOf("→");
                if (index == -1)
                {
                    index = str.IndexOf("|");
                    if (index == -1)
                    {
                        index = str.IndexOf("&");
                        if (index == -1)
                        {
                            //at this point there is no dual part operators
                            index = 0;
                            //we skip blanks chars to get to the first important char
                            SkipBlank(str, ref index);
                            if (str[index] == '!')
                            {
                                //if it's the not operator
                                index++;
                                var eq = new Not();
                                //we have to parse the inner equation everytime the current equation is NOT a constant
                                eq.InternalEquation = Parse(str.Substring(index), currentUnparsed, registeredVars);
                                return eq;
                            }
                            else if (char.IsLetter(str[index]))
                            {
                                //if it starts with a letter, it's a constant
                                var variableName = "";
                                char currentChar = str[index];
                                while (char.IsLetterOrDigit(currentChar) && index < str.Length)
                                {
                                    //we extract the name of the constant
                                    variableName += currentChar;
                                    index++;
                                    if (index < str.Length)
                                        currentChar = str[index];
                                }
                                SkipBlank(str, ref index);
                                if (index < str.Length)
                                    throw new Exception("Unexpected char :" + str[index]);
                                //we add the discovered constant if it doesn't already exists
                                if (!registeredVars.Contains(variableName))
                                    registeredVars.Add(variableName);
                                var eq = new Constant();
                                eq.Name = variableName;
                                return eq;
                            }
                            else if (str[index] == '%')
                            {
                                //if it starts with %, it's a sub tree (parentheses)
                                //we extract the id of the subtree
                                var nbStr = str.Substring(index + 1, 3);
                                index += 4;
                                SkipBlank(str, ref index);
                                if (index < str.Length)
                                    throw new Exception("Unexpected char :" + str[index]);
                                var nextUnparsed = currentUnparsed.subEqs[int.Parse(nbStr)];
                                //aaaand we parse the subtree
                                var eq = Parse(nextUnparsed.str, nextUnparsed, registeredVars);
                                return eq;
                            }
                            else
                                throw new Exception("wrong var starting char");
                        }
                        else
                        {
                            //we have to reverse the string because when there is an equal priority (same operator), we parse from left to right, and not the other way around
                            //if we isolate the first item every time, it means we parse from right to left
                            var strs = new List<string>();
                            foreach (var item in new string(str.Reverse().ToArray()).Split(new string[] { "&" }, 2, StringSplitOptions.None))
                            {
                                strs.Add(new string(item.Reverse().ToArray()));
                            }
                            //and we just fill the parts of the equation, parsing each side
                            var eq = new OperatorEquation();
                            eq.Operator = OperatorEquation.OperatorType.AND;
                            eq.Right = Parse(strs[0], currentUnparsed, registeredVars);
                            eq.Left = Parse(strs[1], currentUnparsed, registeredVars);
                            return eq;
                        }
                    }
                    else
                    {
                        //same things for all the others operators
                        var strs = new List<string>();
                        foreach (var item in new string(str.Reverse().ToArray()).Split(new string[] { "|" }, 2, StringSplitOptions.None))
                        {
                            strs.Add(new string(item.Reverse().ToArray()));
                        }
                        var eq = new OperatorEquation();
                        eq.Operator = OperatorEquation.OperatorType.OR;
                        eq.Right = Parse(strs[0], currentUnparsed, registeredVars);
                        eq.Left = Parse(strs[1], currentUnparsed, registeredVars);
                        return eq;
                    }
                }
                else
                {
                    var strs = new List<string>();
                    foreach (var item in new string(str.Reverse().ToArray()).Split(new string[] { "→" }, 2, StringSplitOptions.None))
                    {
                        strs.Add(new string(item.Reverse().ToArray()));
                    }
                    var eq = new OperatorEquation();
                    eq.Operator = OperatorEquation.OperatorType.INVOLVING;
                    eq.Right = Parse(strs[0], currentUnparsed, registeredVars);
                    eq.Left = Parse(strs[1], currentUnparsed, registeredVars);
                    return eq;
                }
            }
            else
            {
                //<->
                var strs = new List<string>();
                foreach (var item in new string(str.Reverse().ToArray()).Split(new string[] { "*" }, 2, StringSplitOptions.None))
                {
                    strs.Add(new string(item.Reverse().ToArray()));
                }
                var eq = new OperatorEquation();
                eq.Operator = OperatorEquation.OperatorType.EQUIVALENT;
                eq.Right = Parse(strs[0], currentUnparsed, registeredVars);
                eq.Left = Parse(strs[1], currentUnparsed, registeredVars);
                return eq;
            }
        }

        /// <summary>
        /// Increase the index while it has blank chars in the way
        /// </summary>
        /// <param name="str"></param>
        /// <param name="index"></param>
        static public void SkipBlank(string str, ref int index)
        {
            var blankChar = new char[] { ' ', '\n', '\t' };
            while (index < str.Length && blankChar.Contains(str[index]))
                index++;
        }

        #endregion Public Methods
    }
}