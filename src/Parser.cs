

class Parser{
    Stack<Function> functionStack = [];
    public List<Function> functions = [];
    public int anonymousFunctionID = 0;

    static List<List<Token>> SplitByComma(List<Token> tokens){
        List<List<Token>> output = [];
        var start = 0;
        for(var i=0;i<tokens.Count;i++){
            if(tokens[i].type == TokenType.Comma){
                output.Add(tokens[start..i]);
                start = i+1;
            }
        }
        if(start<tokens.Count){
            output.Add(tokens[start..tokens.Count]);
        }
        return output;
    }

    Instruction[] ParseExpressionInParens(Token token){
        return ParseExpression(Tokenizer.Tokenize(token.value));
    }

    Instruction[] ParseExpression(List<Token> tokens){
        var operators = new string[][]{["<", ">"], ["+", "-"], ["/", "*"]};
        if(tokens.Count == 0){
            throw new Exception("No tokens");
        }
        else if(tokens.Count == 1){
            if(tokens[0].type == TokenType.Parens){
                return ParseExpressionInParens(tokens[0]);
            }
            else if(tokens[0].type == TokenType.Float){
                return [new Instruction(JOpcode.F32Const, tokens[0].value)];
            }
            else if(tokens[0].type == TokenType.Int){
                return [new Instruction(JOpcode.I32Const, tokens[0].value)];
            }
            else if(tokens[0].type == TokenType.DoubleQuote){
                return [new Instruction(JOpcode.StrConst, tokens[0].value)];
            }
            else if(tokens[0].type == TokenType.SingleQuote){
                return [new Instruction(JOpcode.CharConst, tokens[0].value)];
            }
            else if(tokens[0].type == TokenType.Varname){
                return [new Instruction(JOpcode.GetLocal, tokens[0].value)];
            }
            else{
                throw new Exception("Unexpected token: "+tokens[0]);
            }
        }
        else if(tokens.Count == 2){
            if(tokens[0].type == TokenType.Operator && tokens[0].value == "!"){
                return [..ParseExpression([tokens[1]]), new Instruction(JOpcode.UnaryOp, "!")];
            }
            else if(tokens[0].type == TokenType.Varname && tokens[1].type == TokenType.Parens){
                var args = SplitByComma(Tokenizer.Tokenize(tokens[1].value)).Select(ParseExpression).ToArray();
                return [.. args.SelectMany(a=>a), new Instruction(JOpcode.Call, tokens[0].value)];
            }
        }
        else if(tokens.Count == 3){
            if(tokens[0].type == TokenType.Varname && tokens[1].type == TokenType.Parens && tokens[2].type == TokenType.Curly){
                var argTokens = SplitByComma(Tokenizer.Tokenize(tokens[1].value));
                var callFunc = GetFunction(tokens[0].value);
                if(callFunc!=null){
                    List<Variable> parameters = [];
                    for(int i=0;i<callFunc.parameters.Length;i++){
                        if(callFunc.parameters[i].type == "DelegateParameter"){
                            parameters.Add(new Variable("Unknown", argTokens[i][0].value));
                            argTokens[i] = [new Token("0", 0, 0, TokenType.Int)];
                        }
                    }
                    var args = argTokens.Select(ParseExpression).ToArray();
                    var anonymousFunc = "__Anonymous__"+anonymousFunctionID;
                    anonymousFunctionID++;
                    
                    var current = new Function(functionStack.Peek(), "void", anonymousFunc, [..parameters]);

                    functionStack.Push(current);
                    current.instructions.AddRange(ParseBody(tokens[2].value));
                    functionStack.Pop();
                    functions.Add(current);

                    return [.. args.SelectMany(a=>a), 
                        new Instruction(JOpcode.Delegate, anonymousFunc), 
                        new Instruction(JOpcode.Call, tokens[0].value)];
                }
                
            }
        }
        foreach(var ops in operators){
            var index = tokens.FindLastIndex(t=>ops.Contains(t.value));
            if(index>=0){
                var left = ParseExpression(tokens[0..index]);
                var right = ParseExpression(tokens[(index+1)..tokens.Count]);
                return [..left, ..right, new Instruction(JOpcode.BinaryOp, tokens[index].value)];
            }
        }
        foreach(var t in tokens){
            Console.WriteLine(t.value);
        }
        throw new Exception("Unexpected tokens");
    }

    List<List<Token>> SplitIntoStatements(string code){
        var tokens = Tokenizer.Tokenize(code);
        var output = new List<List<Token>>();
        var start = 0;
        for(var i=0;i<tokens.Count;i++){
            if(tokens[i].type == TokenType.SemiColon){
                output.Add(tokens[start..i]);
                start=i+1;
            }
            else if(tokens[i].type == TokenType.Curly){
                output.Add(tokens[start..(i+1)]);
                start=i+1;
            }
            else if(tokens[i].type == TokenType.Colon){
                output.Add(tokens[start..(i+1)]);
                start=i+1;
            }
        }
        output.Add(tokens[start..tokens.Count]);
        return output;
    }

    Instruction[] ParseBody(string code){
        var statements = SplitIntoStatements(code);
        List<Instruction> instructions = [];
        for(var i=0;i<statements.Count-1;i++){
            var s = statements[i];
            if(s[1].type == TokenType.Equals){
                instructions.AddRange(ParseExpression(s[2..]));
                instructions.Add(new Instruction(JOpcode.SetLocal, s[0].value));
            }
            else if(s[1].type == TokenType.Colon){
                instructions.Add(new Instruction(JOpcode.Label, s[0].value));
            }
            else if(s[0].type == TokenType.GotoIf){
                instructions.AddRange(ParseExpressionInParens(s[1]));
                instructions.Add(new Instruction(JOpcode.GotoIf, s[2].value));
            }
            else if(s[0].type == TokenType.Goto){
                instructions.Add(new Instruction(JOpcode.Goto, s[1].value));
            }
            else if(s[0].type == TokenType.Var){
                var name = s[1].value;
                var expr = ParseExpression(s[3..]);
                instructions.AddRange(expr);
                instructions.Add(new Instruction(JOpcode.CreateLocal, name));
            }
            else{
                instructions.AddRange(ParseExpression(s));
            }
        }
        var lastStatement = statements[statements.Count-1];
        if(lastStatement.Count == 0){
            return [..instructions];
        }
        else{
            return [..instructions, ..ParseExpression(lastStatement)];
        }
    }

    static Variable[] ParseParameters(string code){
        var parameterTokens = SplitByComma(Tokenizer.Tokenize(code));
        return parameterTokens.Select(p=>new Variable(p[0].value, p[1].value)).ToArray();
    }

    public Parser(string code){
        var tokens = Tokenizer.Tokenize(code);
        var i = 0;
        while(true){
            if(i>=tokens.Count){
                return;
            }
            var returnType = tokens[i];
            var name = tokens[i+1];
            var parameters = ParseParameters(tokens[i+2].value);

            var current = new Function(null, returnType.value, name.value, parameters);
            functionStack.Push(current);
            current.instructions.AddRange(ParseBody(tokens[i+3].value));
            functionStack.Pop();

            functions.Add(current);
            i+=4;
        }
    }

    Function? GetFunction(string name){
        return functions.FirstOrDefault(f=>f.name==name);
    }
}
