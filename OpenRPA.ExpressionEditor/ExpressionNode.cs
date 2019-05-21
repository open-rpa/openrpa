using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.ExpressionEditor
{
    using System.Collections.Generic;

    /// <summary>Represents a node in an auto-completion expression tree.</summary>
    public class ExpressionNode
    {
        /// <summary>Creates a new instance of the <see cref="ExpressionNode" /> class.</summary>
        public ExpressionNode()
        {
            this.Nodes = new List<ExpressionNode>();
        }

        /// <summary>Get or sets a description of the entity expression node represents.</summary>
        public string Description { get; set; }

        /// <summary>Gets or sets the type of item being represented by the node 
        /// (namespace, class, property, method, field, enum, struct, keyword or variable)</summary>
        public string ItemType { get; set; }

        /// <summary>Gets or sets the name of the expression the node represents.</summary>
        public string Name { get; set; }

        /// <summary>Gets or sets the parent node in the tree, or null if the current node is 
        /// the root.</summary>
        public ExpressionNode Parent { get; set; }

        /// <summary>Gets the collection of sub-nodes in the expression tree.</summary>
        public List<ExpressionNode> Nodes { get; private set; }

        /// <summary>Gets the full path of the expression node.</summary>
        public string Path
        {
            get
            {
                return (this.Parent == null) ? this.Name : this.Parent.Path + "." + this.Name;
            }
        }

        /// <summary>Adds a sub-node below the current node.</summary>
        /// <param name="node">The node to add.</param>
        public void Add(ExpressionNode node)
        {
            this.Nodes.Add(node);
        }

        /// <summary>Performs an alphabetic sort of the nodes sub-tree.</summary>
        /// <param name="ascending">True to sort in ascending order, or False for descending order.</param>
        public void Sort(bool ascending = true)
        {
            if (ascending)
            {
                this.Nodes.Sort((x, y) => x.Name.CompareTo(y.Name));
            }
            else // descending order
            {
                this.Nodes.Sort((x, y) => y.Name.CompareTo(x.Name));
            }

            foreach (ExpressionNode subNode in this.Nodes)
            {
                subNode.Sort(ascending);
            }
        }

        /// <summary>Searches for a node within the current sub-tree with the specified path.</summary>
        /// <param name="path">The full path of the expression node.</param>
        public ExpressionNode SearchForNode(string path)
        {
            return ExpressionNode.SearchForNode(this, path);
        }

        /// <summary>Searches an expression tree for a node with the specified path.</summary>
        /// <param name="target">The root node to search within.</param>
        /// <param name="path">The full path to the expression node.</param>
        public static ExpressionNode SearchForNode(ExpressionNode target, string path)
        {
            ExpressionNode match = SearchForNode(target, path, false, false);
            return match ?? target; // return the match, or the supplied target if no match
        }

        /// <summary>Searches an expression tree for a node with the specified path.</summary>
        /// <param name="target">The root node to search within.</param>
        /// <param name="path">The full path to the expression node.</param>
        /// <param name="isNamespace">True to force the node to be a namespace, or False to 
        /// intelligently manage type.</param>
        /// <param name="createIfMissing">True to create any missing nodes en-route to finding 
        /// the node being targeted, and False otherwise.</param>
        public static ExpressionNode SearchForNode(ExpressionNode target, string path,
            bool isNamespace, bool createIfMissing)
        {
            string[] names = path.Split('.');
            string subPath = "";

            if (names.Length > 0 && path.Length > names[0].Length)
            {
                subPath = path.Substring(names[0].Length, path.Length - names[0].Length);
            }

            if (subPath.StartsWith("."))
            {
                subPath = subPath.Substring(1);
            }

            List<ExpressionNode> matches = (from x in target.Nodes
                                            where
x.Name.Equals(names[0], StringComparison.OrdinalIgnoreCase)
                                            select x).ToList();

            if (matches.Count == 0)
            {
                if (!createIfMissing) return null;

                ExpressionNode subNode = new ExpressionNode
                {
                    Name = names[0],
                    ItemType = isNamespace || names.Length > 1 ? "namespace" : "class",
                    Parent = target,
                    Description = isNamespace || names.Length > 1 ? string.Format("Namespace {0}",
                        names[0]) : string.Format("Class {0}", names[0])
                };

                target.Nodes.Add(subNode);

                if (subPath.Trim() != "")
                {
                    return SearchForNode(subNode, subPath, isNamespace, true);
                }

                return subNode;
            }

            // A match was found so search within that sub-tree.
            if (subPath.Trim() != "")
            {
                return SearchForNode(matches[0], subPath, isNamespace, createIfMissing);
            }

            return matches[0];
        }

        /// <summary>Returns a sub-list of first-tier nodes that meet the partial path filter.</summary>
        /// <param name="rootNode">The root node to search within.</param>
        /// <param name="filter">The path name to filter on.</param>
        public static List<ExpressionNode> SubsetAutoCompletionList(ExpressionNode rootNode,
            string filter)
        {
            string parentPath = "";
            string searchTerm = filter;

            if (filter.Contains("."))
            {
                parentPath = filter.Substring(0, filter.LastIndexOf("."));
                searchTerm = filter.Substring(parentPath.Length + 1);
            }

            ExpressionNode targetNode =
                parentPath != "" ? SearchForNode(rootNode, parentPath) : rootNode;
            List<ExpressionNode> matches = new List<ExpressionNode>();

            foreach (ExpressionNode subNode in targetNode.Nodes)
            {
                if (subNode.Name.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(subNode);
                }
            }

            return matches;
        }
    }
}
