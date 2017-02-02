using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Ajax.Utilities;

namespace ArmEngine.Arm.Dom
{
    /// <summary>
    /// AST visitor class for collecting defined and called functions in a script
    /// </summary>
    public class CallNodeVisitor : TreeVisitor
    {
        private readonly HashSet<string> _calledFunctions = new HashSet<string>();

        private readonly HashSet<string> _definedFunctions = new HashSet<string>();

        /// <summary>
        /// Function call
        /// </summary>
        /// <param name="node"></param>
        public override void Visit(CallNode node)
        {
            _calledFunctions.Add(node.Function.ToString());
            base.Visit(node);
        }

        /// <summary>
        /// Function definition
        /// </summary>
        /// <param name="node"></param>
        public override void Visit(FunctionObject node)
        {
            _definedFunctions.Add(node.Binding.Name);
            base.Visit(node);
        }

        public HashSet<string> CalledFunctions => _calledFunctions;

        public HashSet<string> DefinedFunctions => _definedFunctions;
    }
}
