using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using LinqKit;
using Newtonsoft.Json;

namespace Data.Helpers
{
    public class TreeNodeConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            // we can serialize everything that is a TreeNode
            return typeof(TreeNode<T>).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // we currently support only writing of JSON
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // we serialize a node by just serializing the _children dictionary
            var node = value as TreeNode<T>;
            serializer.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            serializer.Serialize(writer, node.Data);
            serializer.Serialize(writer, node.Children);
        }
    }

    public class TreeNode<T> : IEnumerable<TreeNode<T>>
    {

        public T Data { get; set; }
        public int Level { get; set; }
        public TreeNode<T> Parent { get; set; }
        public ICollection<TreeNode<T>> Children { get; set; }

        public TreeNode(T data)
        {
            this.Data = data;
            this.Children = new LinkedList<TreeNode<T>>();
        }

        public TreeNode()
        {

            this.Children = new LinkedList<TreeNode<T>>();
        }

        public TreeNode<T> AddChild(T child, int level)
        {
            TreeNode<T> childNode = new TreeNode<T>(child)
            {
                Parent = this,
                Level = level
            };
            this.Children.Add(childNode);
            return childNode;
        }

        // other features ...
        public IEnumerator<TreeNode<T>> GetEnumerator()
        {
            foreach (TreeNode<T> s in Children)
            {
                yield return s;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class ChildrenWIthLevel<T>
    {
        public bool HasChild { get; set; }
        public T Data { get; set; }
    }

    public class FlatTreeNode<T>
    {

        public T Data { get; set; }
        public int Level { get; set; }
        public TreeNode<T> Parent { get; set; }
        public List<ChildrenWIthLevel<T>> Childrens { get; set; }

        public FlatTreeNode(T data)
        {
            this.Data = data;
            this.Childrens = new List<ChildrenWIthLevel<T>>();
        }

        public FlatTreeNode()
        {

            this.Childrens = new List<ChildrenWIthLevel<T>>();
        }




    }



    public static class LinqHelper
    {

        public static int Level<T>(
           this T item,
           Func<T, T> getParent)
        {


            var parent = getParent(item);
            if (parent == null)
            {
                return 1;
            }
            else
            {
                return GetLevel(parent, getParent, 1);
            }
        }

        private static int GetLevel<T>(
            this T item,
            Func<T, T> getParent, int curlevel)
        {
            var parent = getParent(item);
            if (parent != null)
            {

                return GetLevel(parent, getParent, curlevel + 1);
            }
            else
            {
                return curlevel;
            }

        }





        public static List<FlatTreeNode<T>> GetFlatTree<T>(
           this IEnumerable<T> items,
           Expression<Func<T, ICollection<T>>> getChilds) where T : class
        {
            var tree = new List<FlatTreeNode<T>>();
            AddRootToFlatTree(items, getChilds, tree, 0);
            return tree;
        }

        private static void AddRootToFlatTree<T>(IEnumerable<T> items, Expression<Func<T, ICollection<T>>> getChilds, List<FlatTreeNode<T>> tree, int level) where T : class
        {

            foreach (var item in items)
            {

                var treeitem = new FlatTreeNode<T> { Data = item, Level = level };
                tree.Add(treeitem);

                _GetChildrens(getChilds, item);
                var children = getChilds.Invoke(item);
                foreach (var child in children)
                {
                    _GetChildrens(getChilds, child);
                    var hasChild = getChilds.Invoke(item).Any();
                    var leaf = new ChildrenWIthLevel<T>
                    {
                        Data = child,
                        HasChild = hasChild
                    };
                    treeitem.Childrens.Add(leaf);

                    if (hasChild)
                    {

                        AddLeafToFlatTree<T>(child, getChilds, tree, level + 1);
                    }


                }

            }
        }


        private static void AddLeafToFlatTree<T>(T item, Expression<Func<T, ICollection<T>>> getChilds, List<FlatTreeNode<T>> tree, int level) where T : class
        {

            var treeitem = new FlatTreeNode<T> { Data = item, Level = level };
            tree.Add(treeitem);

            _GetChildrens(getChilds, item);
            var children = getChilds.Invoke(item);
            foreach (var child in children)
            {
                _GetChildrens(getChilds, child);
                var hasChild = getChilds.Invoke(item).Any();
                var leaf = new ChildrenWIthLevel<T>
                {
                    Data = child,
                    HasChild = hasChild
                };
                treeitem.Childrens.Add(leaf);

                if (hasChild)
                {
                    var titem = new FlatTreeNode<T> { Data = child, Level = level };
                    tree.Add(titem);


                    AddLeafToFlatTree<T>(child, getChilds, tree, level + 1);
                }




            }
        }

        public static TreeNode<T> GetTreeWithRoot<T>(
           this IEnumerable<T> allItems,
           Expression<Func<T, ICollection<T>>> getChilds, Expression<Func<T, bool>> rootWhere = null, Expression<Func<T, bool>> @where = null) where T : class
        {
            var stack = new TreeNode<T>();
            IEnumerable<T> items = allItems.Where(rootWhere.Compile());
            AddToTree(items, getChilds, stack, 0, @where);
            return stack;
        }

        public static IEnumerable<TreeNode<T>> GetTree<T>(
           this IEnumerable<T> items,
           Expression<Func<T, ICollection<T>>> getChilds, Expression<Func<T, bool>> @where = null) where T : class
        {
            var list = new List<TreeNode<T>>();

            foreach (var item in items)
            {
                var rootelement = new TreeNode<T>(item)
                {
                    Level = 0,
                    Parent = null
                };

                var current = rootelement;
                IEnumerable<T> children;
                if (@where == null)
                {
                    _GetChildrens(getChilds, item);
                    children = getChilds.Invoke(item);
                }
                else
                {

                    _GetChildrens(getChilds, item);
                    if (getChilds.Invoke(item) == null)
                    {
                        ApplicationDbContext.Current.Entry(item).Collection(getChilds).Query().Where(@where).Load();
                    }
                    children = getChilds.Invoke(item).Where(@where.Compile());

                    //children = getChilds(item).Where(@where);
                }
                AddToTree<T>(children, getChilds, current, 1, @where);
                list.Add(rootelement);
            }


            return list;
        }

        private static void AddToTree<T>(IEnumerable<T> items, Expression<Func<T, ICollection<T>>> getChilds, TreeNode<T> stack, int level, Expression<Func<T, bool>> @where = null) where T : class
        {
            foreach (var item in items)
            {
                var current = stack.AddChild(item, level);
                IEnumerable<T> children;
                if (@where != null)
                {
                    //_GetChildrens(getChilds, item);
                    //if (getChilds.Invoke(item) == null)
                    //{
                    //    ApplicationDbContext.Current.Entry(item).Collection(getChilds).Query().Where(@where).Load();
                    //}
                    children = getChilds.Invoke(item).Where(@where.Compile());
                }
                else
                {
                    //_GetChildrens(getChilds, item);
                    children = getChilds.Invoke(item);
                }
                AddToTree<T>(children, getChilds, current, level + 1);

            }
        }

        public static IEnumerable<Tuple<T, int>> FlattenWithLevel<T>(
            this IEnumerable<T> items,
            Expression<Func<T, ICollection<T>>> getChilds) where T : class
        {
            var stack = new Stack<Tuple<T, int>>();
            foreach (var item in items)
                stack.Push(new Tuple<T, int>(item, 1));

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                yield return current;
                _GetChildrens(getChilds, current.Item1);
                foreach (var child in getChilds.Invoke(current.Item1))
                    stack.Push(new Tuple<T, int>(child, current.Item2 + 1));
            }
        }

        private static void _GetChildrens<T>(Expression<Func<T, ICollection<T>>> getChilds, T item) where T : class
        {
            if (getChilds.Invoke(item) == null)
            {
                ApplicationDbContext.Current.Entry(item).Collection(getChilds).Load();
            }
        }

        public static IEnumerable<T> Flatten<T, R>(this IEnumerable<T> source, Func<T, R> recursion) where R : IEnumerable<T>
        {
            return source.SelectMany(x => (recursion(x) != null && recursion(x).Any()) ? recursion(x).Flatten(recursion) : null)
                         .Where(x => x != null);
        }





        public static IEnumerable<T> Flatten<T>(
        this IEnumerable<T> items,
        Func<T, IEnumerable<T>> getChildren)
        {
            var stack = new Stack<T>();
            foreach (var item in items)
                stack.Push(item);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                yield return current;

                var children = getChildren(current);
                if (children == null) continue;

                foreach (var child in children)
                    stack.Push(child);
            }
        }

        public static bool IsInParent<T>(
        this T item,
        Func<T, T> getParent, Func<T, int?> getId, int parentId)
        {
            if (item == null)
            {
                return false;
            }
            var currId = getId(item);
            if (currId == null)
            {
                return false;
            }
            if (currId == parentId)
            {
                return true;
            }
            else
            {
                return IsInParent(getParent(item), getParent, getId, parentId);
            }

        }
    }
}