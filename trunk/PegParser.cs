/// Dedicated to the public domain by Christopher Diggins
/// http://creativecommons.org/licenses/publicdomain/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Peg
{
    public class Parser
    {
        int mIndex;
        string mData;
        PegAstNode mTree;
        PegAstNode mCur;

        public Parser(string s)
        {
            mIndex = 0;
            mData = s;
            mTree = new PegAstNode("ast", 0, mData, null);
            mCur = mTree;
        }

        public bool AtEnd()
        {
            return mIndex >= mData.Length;
        }

        public int GetPos()
        {
            return mIndex; 
        }

        public string CurrentLine
        {
            get
            {
                return mData.Substring(mIndex, 20);
            }
        }

        public string ParserPosition
        {
            get
            {
                string ret = "";
                int nLine = 0;
                int nLastLineChar = 0;
                for (int i=0; i < mIndex; ++i)
                {
                    if (mData[i].Equals('\n'))
                    {
                        nLine++;
                        nLastLineChar = i;
                    }
                }
                int nCol = mIndex - nLastLineChar;
                ret += "Line " + nLine.ToString() + ", Column " + nCol + "\n";
                
                int nNextLine = mIndex;
                while (nNextLine < mData.Length && !mData[nNextLine].Equals('\n'))
                    nNextLine++;

                ret += mData.Substring(nLastLineChar, nNextLine - nLastLineChar) + "\n";
                ret += new String(' ', nCol);
                ret += "^";
                return ret;
            }
        }

        public void SetPos(int pos)
        {
            mIndex = pos;
        }   

        public void GotoNext()
        {
            if (AtEnd())
            {
                throw new Exception("passed the end of input");
            }
            mIndex++;
        }

        public char GetChar()
        {
            if (AtEnd()) 
            { 
                throw new Exception("passed end of input"); 
            }
            return mData[mIndex];
        }

        public PegAstNode CreateNode(string sLabel)
        {
            Trace.Assert(mCur != null);
            mCur = mCur.Add(sLabel, this);
            Trace.Assert(mCur != null);
            return mCur;
        }

        public void AbandonNode()
        {
            Trace.Assert(mCur != null);
            PegAstNode tmp = mCur;
            mCur = mCur.GetParent();
            Trace.Assert(mCur != null);
            mCur.Remove(tmp);
        }

        public void CompleteNode()
        {
            Trace.Assert(mCur != null);
            mCur.Complete(this);
            mCur = mCur.GetParent();
            Trace.Assert(mCur != null);
        }

        public PegAstNode GetAst()
        {
            return mTree;
        }

        public bool Parse(Peg.Grammar.Rule g)
        {
            bool b = false;
            try
            {
                b = g.Match(this);
            }
            catch (Exception e)
            {
                Console.WriteLine("Parsing error occured with message: " + e.Message);
                Console.WriteLine(ParserPosition);                
            }

            if (b)
            {
                if (mCur != mTree)
                    throw new Exception("internal error: parse tree and parse node do not match after parsing");
                mCur.Complete(this);
            }

            return b;
        }
    }
}
