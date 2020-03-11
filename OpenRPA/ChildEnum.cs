using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA
{
    public static class ChildEnumeratorExtension
    {
        public static Collection<System.Activities.ActivityInstance> getchildList2(this System.Activities.ActivityInstance root) // System.Activities.ActivityInstance.ChildList
        {
            var result = new Collection<System.Activities.ActivityInstance>();
            var childList = root.GetType().GetField("childList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(root);
            if (childList == null) return result;
            var count = (int)childList.GetType().GetField("Count", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(childList);
            if (count == 0) return result;
            if (count == 1)
            {
                var multipleItems = (List<System.Activities.ActivityInstance>)childList.GetType().GetField("multipleItems", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(childList);
                foreach (var item in multipleItems) result.Add(item);
            } else
            {
                var singleItem = (System.Activities.ActivityInstance)childList.GetType().GetField("singleItem", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(childList);
                result.Add(singleItem);

            }
            return result;
        }
        public static bool HasChildren2(this System.Activities.ActivityInstance root)
        {
            var childList = root.GetChildren();
            return childList != null && childList.Count > 0;
        }


        public static Collection<System.Activities.ActivityInstance> GetChildren(this System.Activities.ActivityInstance root)
        {
            //if (!root.HasChildren2())
            //{
            //    return new Collection<System.Activities.ActivityInstance>();
            //}
            var childCache = root.getchildList2() as Collection<System.Activities.ActivityInstance>;
            if(childCache==null) return new Collection<System.Activities.ActivityInstance>();
            return childCache;
        }
        public static System.Activities.ActivityInstance Parent(this System.Activities.ActivityInstance root)
        {
            var p = root.GetType().GetField("parent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(root);
            return p as System.Activities.ActivityInstance;
        }

    }
    class ChildEnumerator : IDisposable, IEnumerator<System.Activities.ActivityInstance>
    {
        private System.Activities.ActivityInstance root;
        private System.Activities.ActivityInstance current;
        private bool initialized;

        public ChildEnumerator(System.Activities.ActivityInstance root)
        {
            this.root = root;
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (!this.initialized)
            {
                this.current = this.root;
                while (this.current.HasChildren2())
                {
                    this.current = this.current.GetChildren()[0];
                }
                this.initialized = true;
                return true;
            }
            if (ReferenceEquals(this.current, this.root))
            {
                return false;
            }
            this.current = this.current.Parent();
            while (this.current.HasChildren2())
            {
                this.current = this.current.GetChildren()[0];
            }
            return true;
        }

        public void Reset()
        {
            this.current = null;
            this.initialized = false;
        }

        public System.Activities.ActivityInstance Current =>
            this.current;

        object IEnumerator.Current =>
            this.Current;
    }

}
