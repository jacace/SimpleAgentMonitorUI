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
    /// <summary>
    /// Custom Exception required by WS-Man
    /// </summary>
    public class AssociationTraversalTypedUtils
    {
        #region CONSTANTS

        // Exceptions consts
        private const string NO_INSTANCES_EXCEPTION = "No instance found.";
        private const string REQUIRED_PARAMETER_MISSING_EXCEPTION = "Reference parameter is missing.";
        private const string GET_FAILURE_EXCEPTION = "More than one instance found.";

        // Managed host profile consts
        private const string DESKTOP_MOBILE_REGISTERED_NAME = "Base Desktop and Mobile";
        private const ushort DMTF_ORGANIZATION = 2;

        #endregion

        #region PRIVATE_DATA_MEMBERS

        private static Uri referenceUri = new Uri("http://schemas.dmtf.org/wbem/wscim/1/*");

        #endregion PRIVATE_DATA_MEMBERS

        #region PUBLIC_FUNCTION

        #region DISCOVER_COMPUTER_SYSTEM

        /// <summary>
        /// Discover the CIM_ComputerSystem object representing the Intel AMT
        /// </summary>
        /// <param name="wsmanClient">the client to connect</param>
        /// <returns>The Intel(r) AMT reference</returns>
        public static CimReference DiscoverIntelAMT(IWSManClient wsmanClient)
        {
            // Define the keys of the Intel AMT object
            CIM_ComputerSystem.CimKeys keys = new CIM_ComputerSystem.CimKeys();
            keys.Name = "Intel(r) AMT";

            // Perform WS-Man enumerate call using the keys
            Collection<CIM_ComputerSystem> computerSystemObj = CIM_ComputerSystem.Enumerate(wsmanClient, keys);

            // return the object - assume that just one instance exist
            return computerSystemObj[0].Reference;
        }

        /// <summary>
        /// Discover the CIM_ComputerSystem object representing the managed host
        /// </summary>
        /// <param name="wsmanClient">the client to connect</param>
        /// <returns>the manged host reference</returns>
        public static CimReference DiscoverManagedHost(IWSManClient wsmanClient)
        {
            // Enumerate all CIM_RegisteredProfile instances
            Collection<CIM_RegisteredProfile> RP_Collection = CIM_RegisteredProfile.Enumerate(wsmanClient);

            if (RP_Collection.Count == 0)
                throw new AssociationTraversalException(NO_INSTANCES_EXCEPTION);

            // This collection will contain the Base Desktop and Mobile classes
            Collection<CIM_RegisteredProfile> collection = new Collection<CIM_RegisteredProfile>();

            // Find the instances by using properties and add it to the collection
            foreach (CIM_RegisteredProfile obj in RP_Collection)
            {
                // Find the relevant instance
                if (obj.RegisteredName == DESKTOP_MOBILE_REGISTERED_NAME &&
                    obj.RegisteredOrganization == DMTF_ORGANIZATION)
                {
                    collection.Add(obj);
                }
            }

            if (collection.Count == 0)
                throw new AssociationTraversalException(NO_INSTANCES_EXCEPTION);

            CIM_RegisteredProfile rp = collection[0];

            // We need the to find the instances that their version is 1.*.* (Means that backward compatibility is maintained)
            // Because it could be more than one instance with this version, 
            // we are looking for the one with the highest version
            foreach (CIM_RegisteredProfile curObj in collection)
            {
                // Find the relevant instance
                // Check if the version string 1.*.*
                bool isCorrectVersion = Regex.IsMatch(curObj.RegisteredVersion, "1*[.][0-9]*[.][0-9]*");

                // Check for the highest version
                if (isCorrectVersion && String.Compare(curObj.RegisteredVersion, rp.RegisteredVersion) > 0)
                    rp = curObj;
            }

            // Traverse the CIM_ComputerSystem (managed host) using the profile we find above as a reference
            CimBase cimBaseObj = GetAssociated(wsmanClient, rp.Reference, typeof(CIM_ComputerSystem), typeof(CIM_ElementConformsToProfile), "ConformantStandard", "ManagedElement");

            return cimBaseObj.Reference;
        }

        #endregion

        #region ENUMERATE_METHODS

        /// <summary>
        /// Return collection of associated classes 
        /// </summary>
        /// <param name="wsmanClient">the client to connect</param>
        /// <param name="EPR">the EPR representing the "main object"</param>
        /// <param name="resultClass">the class name which will be returned from the enumeration</param>
        /// <param name="associationClass">the association class name, this class define the relation between the EPR and the result classes</param>
        /// <returns>Collection of the requested classes</returns>
        public static Collection<CimBase> EnumerateAssociated(IWSManClient wsmanClient, CimReference EPR, Type resultClass, Type associationClass)
        {
            return EnumerateAssociated(wsmanClient, EPR, resultClass, associationClass, null, null);
        }

        /// <summary>
        /// Return collection of associated classes 
        /// </summary>
        /// <param name="wsmanClient">the client to connect</param>
        /// <param name="EPR">the EPR representing the "main object"</param>
        /// <param name="resultClass">the class name which will be returned from the enumeration</param>
        /// <param name="associationClass">the association class name, this class define the relation between the EPR and the result classes</param>
        /// <param name="resultClassPropertiesList">list of properties and their values for getting just the relevant classes</param>
        /// <returns>Collection of the requested classes</returns>
        public static Collection<CimBase> EnumerateAssociated(IWSManClient wsmanClient, CimReference EPR, Type resultClass, Type associationClass, List<KeyValuePair<string, string>> resultClassPropertiesList)
        {
            return EnumerateAssociated(wsmanClient, EPR, resultClass, associationClass, null, null, resultClassPropertiesList);
        }

        /// <summary>
        /// Return collection of associated classes 
        /// </summary>
        /// <param name="wsmanClient">the client to connect</param>
        /// <param name="EPR">the EPR representing the "main object"</param>
        /// <param name="resultClass">the class name which will be returned from the enumeration</param>
        /// <param name="associationClass">the association class name, this class define the relation between the EPR and the result classes</param>
        /// <param name="role">the role name of the "main class"</param>
        /// <param name="resultRole">the role name of the result class</param>
        /// <returns>Collection of the requested classes</returns>
        public static Collection<CimBase> EnumerateAssociated(IWSManClient wsmanClient, CimReference EPR, Type resultClass, Type associationClass, string role, string resultRole)
        {
            // Validate the EPR as it is necessary field
            if (EPR == null)
                throw new AssociationTraversalException(REQUIRED_PARAMETER_MISSING_EXCEPTION);

            // Invoke the special enumeration
            Collection<CimBaseReferencePair> objCollection = EnumerateAssociatedImpl(wsmanClient, EPR, resultClass, associationClass, role, resultRole);

            // Return the collection as CimBase array
            return GetCimBaseFromCollection(objCollection);
        }

        /// <summary>
        /// Return collection of associated classes 
        /// </summary>
        /// <param name="wsmanClient">the client to connect</param>
        /// <param name="EPR">the EPR representing the "main object"</param>
        /// <param name="resultClass">the class name which will be returned from the enumeration</param>
        /// <param name="associationClass">the association class name, this class define the relation between the EPR and the result classes</param>
        /// <param name="role">the role name of the "main class"</param>
        /// <param name="resultRole">the role name of the result class</param>
        /// <param name="resultClassPropertiesList">list of properties and their values for getting just the relevant classes</param>
        /// <returns>Collection of the requested classes</returns>
        public static Collection<CimBase> EnumerateAssociated(IWSManClient wsmanClient, CimReference EPR, Type resultClass, Type associationClass, string role, string resultRole, List<KeyValuePair<string, string>> resultClassPropertiesList)
        {
            // Validate the EPR as it is necessary field
            if (EPR == null)
                throw new AssociationTraversalException(REQUIRED_PARAMETER_MISSING_EXCEPTION);

            // Invoke the special enumeration
            Collection<CimBaseReferencePair> objCollection = EnumerateAssociatedImpl(wsmanClient, EPR, resultClass, associationClass, null, null);

            // If the collection is empty, no need to search for instances - return the collection
            if (objCollection.Count == 0)
                return GetCimBaseFromCollection(objCollection);

            return (resultClassPropertiesList != null) ? GetObjectsByResultPropertyList(objCollection, resultClassPropertiesList) : GetCimBaseFromCollection(objCollection);
        }

        /// <summary>
        /// Return collection of the associations classes 
        /// </summary>
        /// <param name="wsmanClient">the client to connect</param>
        /// <param name="EPR">the EPR representing the "main object"</param>
        /// <param name="resultClass">the association class name which will be returned from the enumeration</param>
        /// <returns>Collection of the requested classes</returns>
        public static Collection<CimBase> EnumerateAssociations(IWSManClient wsmanClient, CimReference EPR, Type resultClass)
        {
            return EnumerateAssociations(wsmanClient, EPR, resultClass, (string)null);
        }

        /// <summary>
        /// Return collection of the associations classes 
        /// </summary>
        /// <param name="wsmanClient">the client to connect</param>
        /// <param name="EPR">the EPR representing the "main object"</param>
        /// <param name="resultClass">the association class name which will be returned from the enumeration</param>
        /// <param name="resultClassPropertiesList">list of properties and their values for getting just the relevant classes</param>
        /// <returns>Collection of the requested classes</returns>
        public static Collection<CimBase> EnumerateAssociations(IWSManClient wsmanClient, CimReference EPR, Type resultClass, List<KeyValuePair<string, string>> resultClassPropertiesList)
        {
            return EnumerateAssociations(wsmanClient, EPR, resultClass, null, resultClassPropertiesList);
        }

        /// <summary>
        /// Return collection of the associations classes 
        /// </summary>
        /// <param name="wsmanClient">the client to connect</param>
        /// <param name="EPR">the EPR representing the "main object"</param>
        /// <param name="role">the role of the association class</param>
        /// <returns>Collection of the requested classes</returns>
        public static Collection<CimBase> EnumerateAssociations(IWSManClient wsmanClient, CimReference EPR, Type resultClass, string role)
        {
            // Validate the EPR as it is necessary field
            if (EPR == null)
                throw new AssociationTraversalException(REQUIRED_PARAMETER_MISSING_EXCEPTION);

            // Invoke the special enumeration
            Collection<CimBaseReferencePair> objCollection = EnumerateAssociationImpl(wsmanClient, EPR, resultClass, role);

            Collection<CimBase> baseCollection = GetCimBaseFromCollection(objCollection);

            return baseCollection;
        }

        /// <summary>
        /// Return collection of the associations classes 
        /// </summary>
        /// <param name="wsmanClient">the client to connect</param>
        /// <param name="EPR">the EPR representing the "main object"</param>
        /// <param name="role">the role of the association class</param>
        /// <param name="resultClassPropertiesList">list of properties and their values for getting just the relevant classes</param>
        /// <returns>Collection of the requested classes</returns>
        public static Collection<CimBase> EnumerateAssociations(IWSManClient wsmanClient, CimReference EPR, Type resultClass, string role, List<KeyValuePair<string, string>> resultClassPropertiesList)
        {
            // Validate the EPR as it is necessary field
            if (EPR == null)
                throw new AssociationTraversalException(REQUIRED_PARAMETER_MISSING_EXCEPTION);

            // Invoke the special enumeration
            Collection<CimBaseReferencePair> objCollection = EnumerateAssociationImpl(wsmanClient, EPR, resultClass, role);

            // If the collection is empty, no need to search for instances - return the collection
            if (objCollection.Count == 0)
                return GetCimBaseFromCollection(objCollection);

            return (resultClassPropertiesList != null) ? GetObjectsByResultPropertyList(objCollection, resultClassPropertiesList) : GetCimBaseFromCollection(objCollection);
        }

        #endregion

        #region GET_METHODS

        /// <summary>
        /// Return a single instance of class (when it is known that a single instance exist)
        /// </summary>
        /// <param name="wsmanClient">the client to connect</param>
        /// <param name="EPR">the EPR representing the "main object"</param>
        /// <param name="resultClass">the class name which will be returned from the enumeration</param>
        /// <param name="associationClass">the association class name, this class define the relation between the EPR and the result classes</param>
        /// <returns>The requested instance</returns>
        public static CimBase GetAssociated(IWSManClient wsmanClient, CimReference EPR, Type resultClass, Type associationClass)
        {
            return GetAssociated(wsmanClient, EPR, resultClass, associationClass, null, null);
        }

        /// <summary>
        /// Return a single instance of class (when it is known that a single instance exist)
        /// </summary>
        /// <param name="wsmanClient">the client to connect</param>
        /// <param name="EPR">the EPR representing the "main object"</param>
        /// <param name="resultClass">the class name which will be returned from the enumeration</param>
        /// <param name="associationClass">the association class name, this class define the relation between the EPR and the result classes</param>
        /// <param name="role">the role name of the "main class"</param>
        /// <param name="resultRole">the role name of the result class</param>
        /// <returns>The requested instance</returns>
        public static CimBase GetAssociated(IWSManClient wsmanClient, CimReference EPR, Type resultClass, Type associationClass, string role, string resultRole)
        {
            Collection<CimBase> objCollection = EnumerateAssociated(wsmanClient, EPR, resultClass, associationClass, role, resultRole);

            if (objCollection.Count > 1)
                throw new AssociationTraversalException(GET_FAILURE_EXCEPTION);

            // If the collection is empty it might be that one of the properties is wrong
            // Or that no instances exist.
            if (objCollection.Count == 0)
                throw new AssociationTraversalException(NO_INSTANCES_EXCEPTION);

            return objCollection[0];
        }

        /// <summary>
        /// Return a single instance of association class (when it is known that a single instance exist)
        /// </summary>
        /// <param name="wsmanClient">the client to connect</param>
        /// <param name="EPR">the EPR representing the "main object"</param>
        /// <param name="resultClass">the class name which will be returned from the enumeration</param>
        /// <returns>The requested instance</returns>
        public static CimBase GetAssociation(IWSManClient wsmanClient, CimReference EPR, Type resultClass)
        {
            return GetAssociation(wsmanClient, EPR, resultClass, null);
        }

        /// <summary>
        /// Return a single instance of association class (when it is known that a single instance exist)
        /// </summary>
        /// <param name="wsmanClient">the client to connect</param>
        /// <param name="EPR">the EPR representing the "main object"</param>
        /// <param name="resultClass">the class name which will be returned from the enumeration</param>
        /// <param name="role">the role name of the "main class"</param>
        /// <returns>The requested instance</returns>
        public static CimBase GetAssociation(IWSManClient wsmanClient, CimReference EPR, Type resultClass, string role)
        {
            Collection<CimBase> objCollection = EnumerateAssociations(wsmanClient, EPR, resultClass, role);

            if (objCollection.Count > 1)
                throw new AssociationTraversalException(GET_FAILURE_EXCEPTION);

            // If the collection is empty it might be that one of the properties is wrong
            // Or that no instances exist.
            if (objCollection.Count == 0)
                throw new AssociationTraversalException(NO_INSTANCES_EXCEPTION);

            return objCollection[0];
        }

        #endregion

        #endregion

        #region PRIVATE_METHODS

        /// <summary>
        /// Invoke the "special enumeration" in order to get collection of classes (by filter)
        /// </summary>
        /// <param name="wsmanClient">the client to connect</param>
        /// <param name="EPR">the EPR representing the "main object"</param>
        /// <param name="resultClassName">the class name which will be returned from the enumeration</param>
        /// <param name="associationClassName">the association class name, this class define the relation between the EPR and the result classes</param>
        /// <param name="role">the role name of the "main class"</param>
        /// <param name="resultRole">the role name of the result class</param>
        /// <returns>Collection of the result classes after executing the enumeration</returns>
        private static Collection<CimBaseReferencePair> EnumerateAssociatedImpl(IWSManClient wsmanClient, CimReference EPR, Type resultClass, Type associationClass, string role, string resultRole)
        {
            EnumerationOptions enumOptions = new EnumerationOptions();

            enumOptions.Filter = new AssociatedFilter(
                EPR,
                (resultClass != null) ? resultClass.UnderlyingSystemType.Name : null, // Validate the resultClass property
                role,
                null, // The includeResultProperty is not supported by AMT
                (associationClass != null) ? associationClass.UnderlyingSystemType.Name : null, // Validate the associationClass property
                resultRole);

            Collection<CimBaseReferencePair> objCollection = CimBase.Enumerate(wsmanClient, enumOptions);

            return objCollection;
        }

        /// <summary>
        /// Invoke the "special enumeration" in order to get collection of association classes (by filter)
        /// </summary>
        /// <param name="wsmanClient">the client to connect</param>
        /// <param name="EPR">the EPR representing the "main object"</param>
        /// <param name="resultClassName">the class name which will be returned from the enumeration</param>
        /// <param name="role">the role name of the "main class"</param>
        /// <returns>Collection of the result classes after executing the enumeration</returns>
        private static Collection<CimBaseReferencePair> EnumerateAssociationImpl(IWSManClient wsmanClient, CimReference EPR, Type resultClass, string role)
        {
            EnumerationOptions enumOptions = new EnumerationOptions();

            enumOptions.Filter = new AssociationFilter(
                EPR,
                resultClass.UnderlyingSystemType.Name,
                role,
                null); // This includeResultProperty is not supported by AMT


            Collection<CimBaseReferencePair> objCollection = CimBase.Enumerate(wsmanClient, enumOptions);

            return objCollection;
        }

        /// <summary>
        /// Return the cim base objects from the collection of CimBaseReferencePair
        /// </summary>
        /// <param name="objCollection"></param>
        /// <returns>Collection that contains the CimBase instance of the CimBaseReferencePair</returns>
        private static Collection<CimBase> GetCimBaseFromCollection(Collection<CimBaseReferencePair> objCollection)
        {
            Collection<CimBase> baseCollection = new Collection<CimBase>();

            foreach (CimBaseReferencePair baseObj in objCollection)
                baseCollection.Add(baseObj.CimBaseObject);
            return baseCollection;
        }

        /// <summary>
        /// Return a list of objects according the list of properties
        /// </summary>
        /// <param name="objCollection">The original collection</param>
        /// <param name="resultClassPropertiesList">The list of properties</param>
        /// <returns></returns>
        private static Collection<CimBase> GetObjectsByResultPropertyList(Collection<CimBaseReferencePair> objCollection, List<KeyValuePair<string, string>> resultClassPropertiesList)
        {
            Collection<CimBase> res = new Collection<CimBase>();

            foreach (CimBaseReferencePair element in objCollection)
            {
                bool add = true;
                foreach (KeyValuePair<string, string> pair in resultClassPropertiesList)
                {
                    if (!element.CimBaseObject.Properties.Contains(pair))
                    {
                        add = false;
                    }
                }
                if (add)
                {
                    res.Add(element.CimBaseObject);
                }
            }
            return res;
        }

        #endregion
    }
}
