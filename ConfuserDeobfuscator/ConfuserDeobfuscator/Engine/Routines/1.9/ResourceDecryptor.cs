using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ConfuserDeobfuscator.Engine.Base;
using ConfuserDeobfuscator.Utils;
using ConfuserDeobfuscator.Utils.Extensions;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Ctx = ConfuserDeobfuscator.Engine.DeobfuscatorContext;

namespace ConfuserDeobfuscator.Engine.Routines._1._9
{
    class ResourceDecryptor : DeobfuscationRoutine19R
    {
        public override string Title
        {
            get { return "Decrypting and restoring resources"; }
        }

        public override bool Detect()
        {
            var cctor = Ctx.Assembly.ManifestModule.Types.FirstOrDefault(x => x.Name == "<Module>").GetStaticConstructor();
            var resolver = Ctx.Assembly.FindMethod(x => IsResourceResolver(x, cctor));

            if (cctor == null)
                return false;

            if (resolver == null)
                return false;

            foreach (var instr in cctor.Body.Instructions)
            {
                if (!instr.PreceedsPattern(cctor.Body, new Predicate<Instruction>[]
                                                           {
                                                               (x => x.OpCode.Code == Code.Ldnull),
                                                               (x =>
                                                                x.OpCode.Code == Code.Ldftn &&
                                                                x.Operand.ToString().Contains(resolver.Name)),
                                                               (x => x.OpCode.Code == Code.Newobj),
                                                               (x => x.OpCode.Code == Code.Callvirt)
                                                           })) continue;
                RoutineVariables.Add("resolverCall", Tuple.Create(instr.GetNextInstructions(cctor.Body, 5).ToArray(), cctor));
                break;
            }


            RoutineVariables.Add("resolver", resolver);
            RoutineVariables.Add("cctor", cctor);

            return true;
        }

        public override void Initialize()
        {

        }

        public override void Process()
        {
            var resolver = RoutineVariables["resolver"] as MethodDef;

            FieldDef badField = null;
            foreach (var fld in Ctx.Assembly.ManifestModule.Types.First(x => x.Name == "<Module>").Fields)
            {
                foreach (var @ref in fld.FindAllReferences())
                {
                    if (@ref.Item2 != resolver) continue;
                    badField = fld;
                }
                if (badField != null)
                    break;
            }
            CalculateMutations(resolver);
            string resName;
            ReadMutatedKeys(resolver, out resName);

            RoutineVariables.Add("badRes", Ctx.Assembly.ManifestModule.Resources.First(r => r.Name == resName));
            RoutineVariables.Add("asmFld", badField);

            foreach (var res in DecryptResources(resName))
                if (res != null)
                {
                    Ctx.Assembly.ManifestModule.Resources.Add(res);
                    Ctx.UIProvider.WriteVerbose("Restored resource: {0}", 2, true, res.Name);
                }
        }

        public override void CleanUp()
        {
            var badCall = RoutineVariables["resolverCall"] as Tuple<Instruction[], MethodDef>;
            var badRes = RoutineVariables["badRes"] as EmbeddedResource;
            var resolver = RoutineVariables["resolver"] as MethodDef;
            var asmFld = RoutineVariables["asmFld"] as FieldDef;

            if (badCall != null)
            {
                foreach (var instr in badCall.Item1)
                    badCall.Item2.Body.Instructions.Remove(instr);
            }

            if (badRes != null)
            {
                Ctx.UIProvider.WriteVerbose("Removed encrypted resource: {0}", 2, true, badRes.Name);
                Ctx.Assembly.ManifestModule.Resources.Remove(badRes);
            }

            if (resolver != null)
            {
                Ctx.UIProvider.WriteVerbose("Removed resource decryptor: {0}::{1}", 2, true, resolver.DeclaringType.Name, resolver.Name);
                resolver.DeclaringType.Methods.Remove(resolver);
            }

            if (asmFld != null)
            {
                Ctx.UIProvider.WriteVerbose("Removed bad field: {0}::{1}", 2, true, asmFld.DeclaringType.Name, asmFld.Name);
                asmFld.DeclaringType.Fields.Remove(asmFld);
            }
        }

        private IEnumerable<EmbeddedResource> DecryptResources(string resName)
        {
            var res = Ctx.Assembly.ManifestModule.Resources.First(x => x.Name == resName) as EmbeddedResource;
            if (res == null)
                throw new Exception("Resource not found: " + resName);

            Stream manifestResourceStream = new MemoryStream(res.GetResourceData());
            var buffer = new byte[manifestResourceStream.Length];
            manifestResourceStream.Read(buffer, 0, buffer.Length);
            var num = (byte) DemutatedKeys["res"].DemutatedInts[0];
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (byte) (buffer[i] ^ num);
                num = (byte) ((num*DemutatedKeys["res"].DemutatedInts[1])%256);
            }
            using (
                var reader =
                    new BinaryReader(new DeflateStream(new MemoryStream(buffer), CompressionMode.Decompress)))
            {
                buffer = reader.ReadBytes(reader.ReadInt32());
                var tmpAsm = AssemblyDef.Load(buffer);
                foreach (var res2 in tmpAsm.ManifestModule.Resources.Where(r => r is EmbeddedResource))
                    yield return res2 as EmbeddedResource;
            }

            yield return null;
        }

        private void ReadMutatedKeys(MethodDef mDef, out string resName)
        {
            resName = mDef.Body.Instructions.GetOperandAt<string>(x => x.OpCode.Code == Code.Ldstr, 0);
            var key1 = mDef.Body.Instructions.FindInstruction(x => x.OpCode.Code == Code.Conv_U1, 0).Previous(mDef.Body).GetLdcI4Value();
            var key2 = mDef.Body.Instructions.FindInstruction(x => x.OpCode.Code == Code.Stelem_I1, 0).Next(mDef.Body).Next(mDef.Body).GetLdcI4Value();

            DemutatedKeys.Add("res", new DemutatedKeys
                                         {
                                             DemutatedInts = new[] {key1, key2}
                                         });
        }

        private bool IsResourceResolver(MethodDef mDef, MethodDef cctor)
        {
            if (!mDef.HasBody)
                return false;

            if (mDef.Parameters.Count != 2 || !mDef.Body.HasVariables)
                return false;

            if (mDef.Parameters[0].Type.FullName != "System.Object" || mDef.Parameters[1].Type.FullName != "System.ResolveEventArgs")
                return false;

            if (!cctor.Body.Instructions.Any(x => x.Operand != null && x.Operand == mDef))
                return false;

            return true;
        }
    }
}
