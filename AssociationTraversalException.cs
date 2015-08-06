using System;
using System.Collections.Generic;
using System.Text;
using Intel.Manageability.WSManagement;
using Intel.Manageability.Cim.Typed;
using Intel.Manageability.Cim;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Intel.Manageability.Cim.Untyped;

namespace Common
{
    public class AssociationTraversalException : Exception
    {
        public AssociationTraversalException(string exceptionString)
            : base(exceptionString)
        {
        }
    }
}
