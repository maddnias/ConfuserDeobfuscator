//
// Pipeline.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2006 Jb Evain
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using ConfuserDeobfuscator.Engine.Base;

namespace ConfuserDeobfuscator.Engine
{
    public class Pipeline
    {
        private readonly ArrayList _steps;

        public Pipeline()
        {
            _steps = new ArrayList();
        }

        public void PrependStep(IDeobfuscationRoutine step)
        {
            _steps.Insert(0, step);
        }

        public void AppendStep(IDeobfuscationRoutine step)
        {
            _steps.Add(step);
        }

        public void AddStepBefore(Type target, IDeobfuscationRoutine step)
        {
            for (int i = 0; i < _steps.Count; i++)
            {
                if (target.IsInstanceOfType(_steps[i]))
                {
                    _steps.Insert(i, step);
                    return;
                }
            }
            string msg = String.Format("Step {0} could not be inserted before (not found) {1}", step, target);
            throw new InvalidOperationException(msg);
        }

        public void ReplaceStep(Type target, IDeobfuscationRoutine step)
        {
            AddStepBefore(target, step);
            RemoveStep(target);
        }

        public void AddStepAfter(Type target, IDeobfuscationRoutine step)
        {
            for (int i = 0; i < _steps.Count; i++)
            {
                if (target.IsInstanceOfType(_steps[i]))
                {
                    if (i == _steps.Count - 1)
                        _steps.Add(step);
                    else
                        _steps.Insert(i + 1, step);
                    return;
                }
            }
            string msg = String.Format("Step {0} could not be inserted after (not found) {1}", step, target);
            throw new InvalidOperationException(msg);
        }

        public void AddStepAfter(IDeobfuscationRoutine target, IDeobfuscationRoutine step)
        {
            for (int i = 0; i < _steps.Count; i++)
            {
                if (_steps[i] == target)
                {
                    if (i == _steps.Count - 1)
                        _steps.Add(step);
                    else
                        _steps.Insert(i + 1, step);
                    return;
                }
            }
        }

        public void RemoveStep(Type target)
        {
            for (int i = 0; i < _steps.Count; i++)
            {
                if (_steps[i].GetType() != target)
                    continue;

                _steps.RemoveAt(i);
                break;
            }
        }

        public void Process()
        {
            while (_steps.Count > 0)
            {
                var step = (IDeobfuscationRoutine)_steps[0];
                step.Initialize();

                if (step.Detect())
                {
                    if (DeobfuscatorContext.LoggingLevel == DeobfuscatorContext.OutputLevel.Verbose)
                        DeobfuscatorContext.UIProvider.Write("\n-----------------------\n" + step.Title);
                    else
                        DeobfuscatorContext.UIProvider.Write(step.Title);
                    step.Process();
                    DeobfuscatorContext.UIProvider.WriteVerbose("\nCleaning up");
                    step.CleanUp();
                    step.FinalizeCleanUp();
                    if (step is IFileRewriter)
                        (step as IFileRewriter).ReloadFile();
                }
                _steps.Remove(step);
            }
        }

        public IDeobfuscationRoutine[] GetSteps()
        {
            return (IDeobfuscationRoutine[])_steps.ToArray(typeof(IDeobfuscationRoutine));
        }

        public bool ContainsStep(Type type)
        {
            foreach (IDeobfuscationRoutine step in _steps)
                if (step.GetType() == type)
                    return true;

            return false;
        }
    }
}
