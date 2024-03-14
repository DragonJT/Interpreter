enum TokenType{Varname, Int, Float, Curly, Square, Parens, Punctuation, DoubleQuote, SingleQuote}

class Token(string value, int start, int end, TokenType type){
    public string value = value;
    public int start = start;
    public int end = end;
    public TokenType type = type;
}

static class Tokenizer{
    public static List<Token> Tokenize(string code){
        var index = 0;
        var tokens = new List<Token>();
        var open = "({[";
        var close = ")}]";

        loop:
        if(index>=code.Length){
            return tokens;
        }
        var c = code[index];
        if(char.IsWhiteSpace(c)){
            index++;
            goto loop;
        }
        if(char.IsLetter(c) || c=='_'){
            var start = index;
            index++;
            while(true){
                if(index>=code.Length){
                    tokens.Add(new Token(code[start..index], start, index, TokenType.Varname));
                    return tokens;
                }
                c = code[index];
                if(char.IsLetter(c) || c=='_' || char.IsDigit(c)){
                    index++;
                    continue;
                }
                tokens.Add(new Token(code[start..index], start, index, TokenType.Varname));
                goto loop;
            }
        }
        else if(char.IsDigit(c)){
            var start = index;
            index++;
            var type = TokenType.Int;
            while(true){
                if(index>=code.Length){
                    tokens.Add(new Token(code[start..index], start, index, type));
                    return tokens;
                }
                c = code[index];
                if(c=='.'){
                    type = TokenType.Float;
                    index++;
                    continue;
                }
                else if(char.IsDigit(c)){
                    index++;
                    continue;
                }
                tokens.Add(new Token(code[start..index], start, index, type));
                goto loop;
            }
        }
        else if(open.Contains(c)){
            var start = index;
            var depth = 1;
            index++;
            while(true){
                if(index>=code.Length){
                    throw new Exception("No close braces before end of file");
                }
                c = code[index];
                if(open.Contains(c)){
                    index++;
                    depth++;
                    continue;
                }
                else if(close.Contains(c)){
                    index++;
                    depth--;
                    if(depth<1){
                        if(code[start] == '{' && code[index-1]=='}'){
                            tokens.Add(new Token(code[(start+1)..(index-1)], start, index, TokenType.Curly));
                            goto loop;
                        }
                        else if(code[start] == '[' && code[index-1]==']'){
                            tokens.Add(new Token(code[(start+1)..(index-1)], start, index, TokenType.Square));
                            goto loop;
                        }
                        else if(code[start] == '(' && code[index-1]==')'){
                            tokens.Add(new Token(code[(start+1)..(index-1)], start, index, TokenType.Parens));
                            goto loop;
                        }
                        else{
                            throw new Exception("Unexpected start and end of braces: "+code[start]+code[index-1]);
                        }
                    }
                    continue;
                }
                index++;
            }
        }
        else if(c == '"'){
            var start = index;
            index++;
            while(true){
                if(index>=code.Length){
                    throw new Exception("Expecting end of doublequote");
                }
                c = code[index];
                if(c=='\\'){
                    index+=2;
                    continue;
                }
                else if(c=='"'){
                    index++;
                    tokens.Add(new Token(code[(start+1)..(index-1)], start, index, TokenType.DoubleQuote));
                    goto loop;
                }
                index++;
            }
        }
        else if(c == '\''){
            var start = index;
            index++;
            while(true){
                if(index>=code.Length){
                    throw new Exception("Expecting end of singlequote");
                }
                c = code[index];
                if(c=='\\'){
                    index+=2;
                    continue;
                }
                else if(c=='\''){
                    index++;
                    tokens.Add(new Token(code[(start+1)..(index-1)], start, index, TokenType.SingleQuote));
                    goto loop;
                }
                index++;
            }
        }
        else{
            tokens.Add(new Token(code[index].ToString(), index, index+1, TokenType.Punctuation));
            index++;
            goto loop;
        }
    }
}