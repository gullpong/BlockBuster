using System;
using System.Collections.Generic;

namespace BlockBuster.Core
{
    public class ObjectPool
    {
        public List<Object> Objects { get; private set; }

        public ObjectPool()
        {
            this.Objects = new List<Object>();
        }

        public void Reset()
        {
            while (this.Objects.Count > 0)
            {
                var obj = this.Objects[0];
                obj.Release(obj);
            }
        }

        public void Add(Object obj)
        {
            this.Objects.Add(obj);
            obj.Release += o =>
            {
                this.Objects.Remove(o);
            };
        }

        public void Do()
        {
            var todoObjs = new Queue<Object>(this.Objects);
            var doneObjs = new List<Object>();
            var deadObjs = new List<Object>();
            while (todoObjs.Count > 0)
            {
                var obj = todoObjs.Dequeue();
                bool allDone = true;
                foreach (var dep in obj.Dependents)
                {
                    if (!doneObjs.Contains(dep))
                    {
                        allDone = false;
                        break;
                    }
                }
                if (allDone)
                {
                    if (!obj.Do())
                        deadObjs.Add(obj);
                    doneObjs.Add(obj);
                }
                else
                {
                    todoObjs.Enqueue(obj);
                }
            }
            foreach (var obj in deadObjs)
                obj.Release(obj);
        }
    }

    public interface IObjectAnimation
    {
        bool IsBusy();

        void Link(Object model);
        void Unlink();
        void Animate();
    }

    public class Object
    {
        protected ObjectPool pool;
        protected IObjectAnimation animation;

        public List<Object> Dependents { get; protected set; }
        public Action<Object> Release { get; set; }

        public Object(ObjectPool pool)
        {
            this.Dependents = new List<Object>();
            this.pool = pool;
            this.pool.Add(this);
        }

        public void Animate(IObjectAnimation animation)
        {
            this.animation = animation;
            this.animation.Link(this);
            this.Release += o =>
            {
                this.animation.Unlink();
            };
        }

        public void Depend(Object obj)
        {
            this.Dependents.Add(obj);
            obj.Release += o =>
            {
                this.Dependents.Remove(obj);
            };
        }

        public virtual bool IsReady()
        {
            return true;
        }

        public virtual bool Do()
        {
            if (this.animation != null)
                this.animation.Animate();

            // Returning 'false' would remove this Object.
            return true;
        }
    }
}
