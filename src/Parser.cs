

static class Parser{
    static List<List<Token>> SplitByComma(List<Token> tokens){
        List<List<Token>> output = [];
        var start = 0;
        for(var i=0;i<tokens.Count;i++){
            if(tokens[i].value == ","){
                output.Add(tokens[start..i]);
                start = i+1;
            }
        }
        if(start<tokens.Count){
            output.Add(tokens[start..tokens.Count]);
        }
        return output;
    }

    static Instruction[] ParseExpressionInParens(Token token){
        return ParseExpression(Tokenizer.Tokenize(token.value));
    }

    public static Instruction[] ParseExpression(List<Token> tokens){
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
            if(tokens[0].type == TokenType.Varname && tokens[1].type == TokenType.Parens){
                var args = SplitByComma(Tokenizer.Tokenize(tokens[1].value)).Select(ParseExpression).ToArray();
                return [.. args.SelectMany(a=>a).ToArray(), new Instruction(JOpcode.Call, tokens[0].value)];
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

    static Instruction[] ParseBody(string code){
        var tokens = Tokenizer.Tokenize(code);
        List<Instruction> instructions = [];
        var i = 0;
        while(true){
            if(i>=tokens.Count){
                return [..instructions];
            }
            if(i+1<tokens.Count && tokens[i+1].value == "="){
                var end = tokens.FindIndex(i+2, t=>t.value == ";");
                if(end<0){
                    throw new Exception("No end of expression");
                }
                instructions.AddRange(ParseExpression(tokens[(i+2)..end]));
                instructions.Add(new Instruction(JOpcode.SetLocal, tokens[i].value));
                i = end+1;
            }
            else if(tokens[i].value == "return"){
                var end = tokens.FindIndex(i+1, t=>t.value == ";");
                if(end<0){
                    throw new Exception("No end of expression");
                }
                instructions.AddRange(ParseExpression(tokens[(i+1)..end]));
                i = end+1;
            }
            else if(tokens[i+1].value == ":"){
                instructions.Add(new Instruction(JOpcode.Label, tokens[i].value));
                i+=2;
            }
            else if(tokens[i].value == "goto_if"){
                instructions.AddRange(ParseExpressionInParens(tokens[i+1]));
                instructions.Add(new Instruction(JOpcode.GotoIf, tokens[i+2].value));
                i+=4;
            }
            else if(tokens[i].value == "goto"){
                instructions.Add(new Instruction(JOpcode.Goto, tokens[i+1].value));
                i+=3;
            }
            else if(tokens[i].value == "var"){
                var name = tokens[i+1].value;
                var end = tokens.FindIndex(i+3, t=>t.value == ";");
                if(end<0){
                    throw new Exception("No end of expression");
                }
                var expr = ParseExpression(tokens[(i+3)..end]);
                instructions.AddRange(expr);
                instructions.Add(new Instruction(JOpcode.CreateLocal, name));
                i = end+1;
            }
            else{
                var end = tokens.FindIndex(i, t=>t.value == ";");
                if(end<0){
                    throw new Exception("No end of expression");
                }
                instructions.AddRange(ParseExpression(tokens[i..end]));
                i = end+1;
            }
        }
    }

    static Parameter[] ParseParameters(string code){
        var parameterTokens = SplitByComma(Tokenizer.Tokenize(code));
        return parameterTokens.Select(p=>new Parameter(p[0].value, p[1].value)).ToArray();
    }

    public static Function[] ParseFunctions(string code){
        var tokens = Tokenizer.Tokenize(code);
        List<Function> functions = [];
        var i = 0;
        while(true){
            if(i>=tokens.Count){
                return [..functions];
            }
            var returnType = tokens[i];
            var name = tokens[i+1];
            var parameters = ParseParameters(tokens[i+2].value);
            var body = ParseBody(tokens[i+3].value);
            functions.Add(new Function(returnType.value, name.value, parameters, body));
            i+=4;
        }
    }
}
