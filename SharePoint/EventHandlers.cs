/* Copyright (C) 2008-2011, Manuel Meitinger
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 2 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;

[assembly: AssemblyTitle("Jour Fix SharePoint Feature")]
[assembly: AssemblyDescription("Windows SharePoint Services Feature für Jour Fix Webs")]
[assembly: AssemblyProduct("Jour Fix")]
[assembly: AssemblyCompany("Aufbauwerk der Jugend")]
[assembly: AssemblyCopyright("Copyright © 2008-2011 by Aufbauwerk der Jugend")]
[assembly: AssemblyVersion("1.0.1.0")]

namespace Aufbauwerk.JourFix.SharePoint
{
    public abstract class Common : SPItemEventReceiver
    {
        protected abstract int ResolvePrincipalID(SPItemEventProperties properties);

        protected void AdjustPermissions(SPItemEventProperties properties)
        {
            // catch all exceptions and report them properly
            try
            {
                // get the principal id
                int principalID = ResolvePrincipalID(properties);

                // do all the remaining tasks under elevated privileges
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    // open the web (and site) that contains the item within the system account context
                    using (SPSite site = new SPSite(properties.SiteId))
                    using (SPWeb web = site.OpenWeb(properties.RelativeWebUrl))
                    {
                        // resolve the id into an actual object
                        SPPrincipal principal = null;
                        if (principalID != -1)
                        {
                            try { principal = web.SiteUsers.GetByID(principalID); }
                            catch (Exception e)
                            {
                                try { principal = web.SiteGroups.GetByID(principalID); }
                                catch { throw e; }
                            }
                        }

                        // locate the added/updated item and break the role inheritance if necessary
                        SPListItem item = web.Lists[properties.ListId].GetItemById(properties.ListItemId);
                        if (!item.HasUniqueRoleAssignments)
                            item.BreakRoleInheritance(true);

                        // initialize the assignment found flag and the contributor role
                        bool assignmentForPrincipalFound = false;
                        SPRoleDefinition contributorRole = web.RoleDefinitions.GetByType(SPRoleType.Contributor);

                        // adjust all existing role assignments
                        SPRoleAssignmentCollection assignments = item.RoleAssignments;
                        List<int> removeAssignments = new List<int>(assignments.Count);
                        foreach (SPRoleAssignment assignment in assignments)
                        {
                            // check if there is at least one role with web designer permissions
                            bool hasWebDesignerPermission = false;
                            foreach (SPRoleDefinition definition in assignment.RoleDefinitionBindings)
                            {
                                if (definition.Type >= SPRoleType.WebDesigner)
                                {
                                    hasWebDesignerPermission = true;
                                    break;
                                }
                            }

                            // handle assignments for the target principal differently
                            int memberID = assignment.Member.ID;
                            if (memberID == principalID)
                            {
                                // replace all roles with the contributor role if the principal doesn't have web designer permissions
                                if (!hasWebDesignerPermission)
                                {
                                    assignment.RoleDefinitionBindings.RemoveAll();
                                    assignment.RoleDefinitionBindings.Add(contributorRole);
                                    assignment.Update();
                                }
                                assignmentForPrincipalFound = true;
                            }
                            else
                            {
                                // mark the assignment for deletion if there's no role with at least web designer permissions
                                if (!hasWebDesignerPermission)
                                    removeAssignments.Add(memberID);
                            }
                        }

                        // add the contributor role assignment if necessary
                        if (!assignmentForPrincipalFound)
                        {
                            SPRoleAssignment assignment = new SPRoleAssignment(principal);
                            assignment.RoleDefinitionBindings.Add(contributorRole);
                            assignments.Add(assignment);
                        }

                        // remove all undesired role assignments
                        foreach (int id in removeAssignments)
                            assignments.RemoveById(id);
                    }
                });
            }
            catch (Exception e)
            {
                // report the error
                properties.Cancel = true;
                properties.ErrorMessage = e.Message;
            }
        }

        public override void ItemAdded(SPItemEventProperties properties)
        {
            // add the item and adjust it's permissions
            base.ItemAdded(properties);
            AdjustPermissions(properties);
        }

        public override void ItemUpdated(SPItemEventProperties properties)
        {
            // update the item and adjust it's permissions
            base.ItemUpdated(properties);
            AdjustPermissions(properties);
        }
    }

    public class DocumentLibrary : Common
    {
        protected override int ResolvePrincipalID(SPItemEventProperties properties)
        {
            // extract the principal id from the organizational-number field
            string value = (string)properties.ListItem[SPBuiltInFieldId.OrganizationalIDNumber];
            if (string.IsNullOrEmpty(value))
                return -1;
            return int.Parse(value);
        }
    }

    public class Tasks : Common
    {
        protected override int ResolvePrincipalID(SPItemEventProperties properties)
        {
            // either extract the principal id directly from the assigned-to field or try to resolve the field's text
            string value = (string)properties.ListItem[SPBuiltInFieldId.AssignedTo];
            if (string.IsNullOrEmpty(value))
                return -1;
            int index = value.IndexOf(';');
            return index != -1 ?
                int.Parse(value.Substring(0, index)) :
                SPUtility.ResolvePrincipal(properties.ListItem.Web, value, SPPrincipalType.All, SPPrincipalSource.All, null, false).PrincipalId;
        }
    }
}
