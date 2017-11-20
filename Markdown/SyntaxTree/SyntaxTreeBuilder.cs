﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Markdown.Lang;
using Markdown.Parser;

namespace Markdown.SyntaxTree
{
    class SyntaxTreeBuilder
    {
        public Tree<IToken> Tree => root;
        private readonly Dictionary<string, Func<IToken>> tags;
        private readonly Stack<IToken> stack = new Stack<IToken>();
        private readonly Tree<IToken> root = new Tree<IToken>();
        private readonly SyntaxTreeValidator validator;

        private Tree<IToken> current;

        public SyntaxTreeBuilder(Dictionary<string, Func<IToken>> tagsDictionary)
        {
            tags = tagsDictionary;
            current = root;
            validator = new SyntaxTreeValidator();
        }

        public void CloseNotPairedTags()
        {
            var stackList = stack.ToList();

            var toRemove = stack.Where(token => !token.HasClosingTag);
            foreach (var token in toRemove)
            {
                stackList.Remove(token);
                if (current.Value == token)
                    current = current.Parent;
            }
                
            stack.Clear();
            stackList.Reverse();

            foreach (var token in stackList)
                stack.Push(token);
        }

        public void Append(IMatchResult matchResult)
        {
            if (stack.Count == 0)
            {
                if (matchResult is Match)
                    SetNewCurrentAndAddTag((Match)matchResult, nesting: false);
                else
                    current.Add(new TagContent(matchResult.Data));
            }
            else
            {
                if (matchResult is Match)
                {   
                    if (stack.Peek().MdTag == matchResult.Data)
                        CloseCurrentTag((Match)matchResult);
                    else
                        SetNewCurrentAndAddTag((Match)matchResult, nesting: true);
                }
                else
                    current.Value.Content.Add(new TagContent(matchResult.Data));
            }
        }

        private void CloseCurrentTag(Match matchResult)
        {
            var tag = tags[matchResult.Data]();

            //TODO вынести проверку 
            if (!(tag.IsCorrectSurroundingsForClosingTag(matchResult.PrevSymbol, matchResult.NextSymbol)
                && tag.IsCorrectNesting(current.Parent.Value)))
                current.Add(new TagContent(matchResult.Data));
            else
            {
                current = current.Parent;
                stack.Pop();
            }
        }

        private void SetNewCurrentAndAddTag(Match matchResult, bool nesting)
        {
            var tag = tags[matchResult.Data]();
            //TODO вынести проверку 
            if (!tag.IsCorrectSurroundingsForOpeningTag(matchResult.PrevSymbol, matchResult.NextSymbol))
                tag = new TagContent(matchResult.Data);
            if (nesting)
            {
                //TODO и вооьще сделать эту фигню красивой
                if (!tag.IsCorrectNesting(current.Value))
                {
                    tag = new TagContent(matchResult.Data);
                    current.Value.Content.Add(tag);
                }
                else
                {
                    stack.Push(tag);
                    current.Value.Content.Add(tag);
                    current = current.Add(tag);
                }
            }
            else
            {
                stack.Push(tag);
                current = current.Add(tag);
            }
            
        }
    }
}