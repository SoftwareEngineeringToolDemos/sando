﻿using System;
using System.Diagnostics.Contracts;

namespace Sando.ExtensionContracts.ProgramElementContracts
{
	public class StructElement : ProgramElement
	{
        public StructElement(string name, int definitionLineNumber, int definitionColumnNumber, string fullFilePath, string snippet, AccessLevel accessLevel,
            string namespaceName, string body, string extendedStructs, string modifiers)
            : base(name, definitionLineNumber, definitionColumnNumber, fullFilePath, snippet)
		{
			Contract.Requires(namespaceName != null, "StructElement:Constructor - namespace cannot be null!");
			Contract.Requires(extendedStructs != null, "StructElement:Constructor - extended structs cannot be null!");
			
			AccessLevel = accessLevel;
            Namespace = namespaceName;
            Body = body;
			ExtendedStructs = extendedStructs;
			Modifiers = modifiers;
		}

		public virtual AccessLevel AccessLevel { get; private set; }
        public virtual string Namespace { get; private set; }
        public virtual string Body { get; private set; }
		public virtual string ExtendedStructs { get; private set; }
		public virtual string Modifiers { get; private set; }
		public override ProgramElementType ProgramElementType { get { return ProgramElementType.Struct; } }
	}
}
