/// Public domain code by Christopher Diggins
/// http://www.cat-language.com

using System;
using System.Collections.Generic;
using System.Text;
using Peg;

namespace Cat
{
    public class CatGrammar : Grammar
    {
        public static Rule CatIdentChar()
        {
            return Choice(IdentNextChar(), CharSet("~`!@#$%^&*-+=|\\:;<>.?/"));
        }
        public static Rule CatIdent()
        {
            return Plus(CatIdentChar()); ;
        }
        public static Rule Token(string s) 
        { 
            return Token(CharSeq(s)); 
        }
        public static Rule Token(Rule r) 
        {
            return Seq(r, WS()); 
        }
        public static Rule Word(string s) 
        {
            return Seq(CharSeq(s), EOW(), WS()); 
        }
        public static Rule Quote() 
        {
            // Note the usage of Delay which breaks circular references in the grammar
            return AstNode("quote", Seq(Token("["), Star(Delay(Expr)), NoFail(Token("]"), "missing ']'"))); 
        }
        public static Rule IntegerLiteral() 
        { 
            return AstNode("int", Seq(Opt(SingleChar('-')), Plus(Digit()), Not(CharSet(".")))); 
        }
        public static Rule EscapeChar() 
        { 
            return Seq(SingleChar('\\'), AnyChar()); 
        }
        public static Rule StringCharLiteral() 
        { 
            return Choice(EscapeChar(), NotChar('"')); 
        }
        public static Rule CharLiteral() 
        {
            return AstNode("char", Seq(SingleChar('\''), StringCharLiteral(), SingleChar('\'')));
        }
        public static Rule StringLiteral() 
        { 
            return AstNode("string", Seq(SingleChar('\"'), Star(StringCharLiteral()), SingleChar('\"'))); 
        }
        public static Rule FloatLiteral() 
        {
            return AstNode("float", Seq(Opt(SingleChar('-')), Plus(Digit()), SingleChar('.'), Plus(Digit()))); 
        }
        public static Rule HexValue()
        {
            return AstNode("hex", Plus(HexDigit()));
        }
        public static Rule HexLiteral()
        {
            return Seq(CharSeq("0x"), NoFail(HexValue(), "expected at least one hexadecimal digit"));
        }
        public static Rule BinaryValue()
        {
            return AstNode("bin", Plus(BinaryDigit()));
        }
        public static Rule BinaryLiteral()
        {
            return Seq(CharSeq("0b"), NoFail(BinaryValue(), "expected at least one binary digit"));
        }
        public static Rule NumLiteral()
        {            
            return Choice(HexLiteral(), BinaryLiteral(), FloatLiteral(), IntegerLiteral());
        }
        public static Rule Literal() 
        {
            return Choice(StringLiteral(), CharLiteral(), NumLiteral()); 
        }        
        public static Rule Symbol() 
        { 
            // The "()" together is treated as a single symbol
            return Choice(CharSeq("()"), CharSet("(),")); 
        }
        public static Rule Name() 
        {
            return Token(AstNode("name", Choice(Symbol(), CatIdent()))); 
        }
        public static Rule Expr()
        {
            return Token(Choice(Literal(), Quote(), Name()));
        }
        public static Rule CodeBlock()
        {
            return Seq(Token("{"), Star(Expr()), NoFail(Token("}"), "missing '}'"));
        }
        public static Rule Param()
        {
            return Token(AstNode("param", Ident()));
        }
        public static Rule Params()
        {
            return Seq(Token("("), Star(Param()), NoFail(Token(")"), "missing ')'"));
        }
        public static Rule TypeVar()
        {
            return AstNode("type_var", Seq(LowerCaseLetter(), Star(IdentNextChar())));
        }
        public static Rule StackVar()
        {
            return AstNode("stack_var", Seq(UpperCaseLetter(), Star(IdentNextChar())));
        }
        public static Rule TypeOrStackVar()
        {
            return Seq(SingleChar('\''), NoFail(Choice(TypeVar(), StackVar()), "invalid type or stack variable name"), WS());
        }
        public static Rule TypeName()
        {
            return Token(AstNode("type_name", Ident()));
        }
        public static Rule TypeAlias()
        {
            return Token(Seq(Ident(), Token("="), Delay(TypeComponent)));
        }
        public static Rule TypeComponent()
        {
            return Choice(TypeAlias(), TypeName(), TypeOrStackVar(), Delay(FxnType));
        }
        public static Rule Production()
        {
            return AstNode("stack", Token(Star(TypeComponent())));
        }
        public static Rule Consumption()
        {
            return AstNode("stack", Token(Star(TypeComponent())));
        }
        public static Rule Arrow()
        {
            return AstNode("arrow", Choice(Token("->"), Token("~>")));
        }
        public static Rule FxnType()
        {
            return AstNode("type_fxn", Seq(Token("("), Production(), NoFail(Arrow(), "expected either -> or ~>"), Consumption(), NoFail(Token(")"), "expected closing paranthesis")));
        }
        public static Rule TypeDecl()
        {
            return Seq(Token(":"), NoFail(FxnType(), "expected function type declaration"), WS());
        }
        public static Rule FxnDef()
        {
            return AstNode("def", Seq(Word("define"), NoFail(Name(), "expected name"),
                Opt(Params()), Opt(TypeDecl()), NoFail(CodeBlock(), "expected a code block")));
        }
        #region macros
        public static Rule MacroTypeVar()
        {
            return AstNode("macro_type_var", Seq(LowerCaseLetter(), Star(IdentNextChar())));
        }
        public static Rule MacroStackVar()
        {
            return AstNode("macro_stack_var", Seq(UpperCaseLetter(), Star(IdentNextChar())));
        }
        public static Rule MacroVar()
        {
            return Seq(SingleChar('$'), NoFail(Choice(MacroTypeVar(), MacroStackVar()), "expected a valid macro type variable or stack variable"));
        }
        public static Rule MacroName()
        {
            return AstNode("macro_name", Choice(Symbol(), CatIdent()));
        }
        public static Rule MacroTerm()
        {
            return Token(Choice(MacroQuote(), MacroVar(), MacroName()));
        }
        public static Rule MacroQuote()
        {
            return AstNode("macro_quote", Seq(Token("["), Star(Delay(MacroTerm)), NoFail(Token("]"), "missing ']'")));
        }
        public static Rule MacroPattern()
        {
            return AstNode("macro_pattern", Seq(Token("{"), Star(MacroTerm()), NoFail(Token("}"), "missing '}'")));
        }
        public static Rule MacroDef()
        {
            return AstNode("macro", Seq(Word("macro"), NoFail(Seq(MacroPattern(), Token("=>"), MacroPattern()), "expected macro defintiion")));
        }
        #endregion
        public static Rule Line()
        {
            return Seq(WS(), Star(Choice(FxnDef(), MacroDef(), Expr())));
        }
        public static Rule PlainCatProgram()
        {
            return Star(Token(FxnDef()));
        }
        public static Rule MetaCatProgram()
        {
            return Star(Choice(Token(FxnDef()), Token(MacroDef())));
        }
    }
}
