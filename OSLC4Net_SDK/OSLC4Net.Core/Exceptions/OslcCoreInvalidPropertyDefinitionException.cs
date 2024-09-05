/*******************************************************************************
 * Copyright (c) 2012 IBM Corporation.
 *
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * and Eclipse Distribution License v. 1.0 which accompanies this distribution.
 *
 * The Eclipse Public License is available at http://www.eclipse.org/legal/epl-v10.html
 * and the Eclipse Distribution License is available at
 * http://www.eclipse.org/org/documents/edl-v10.php.
 *
 * Contributors:
 *     Steve Pitschke  - initial API and implementation
 *******************************************************************************/

using System;
using System.Reflection;

using OSLC4Net.Core.Attribute;

namespace OSLC4Net.Core.Exceptions;

public class OslcCoreInvalidPropertyDefinitionException : OslcCoreApplicationException
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="resourceType"></param>
    /// <param name="method"></param>
    /// <param name="oslcPropertyDefinition"></param>
    public OslcCoreInvalidPropertyDefinitionException(Type resourceType, MethodInfo method, OslcPropertyDefinition oslcPropertyDefinition) :
        base(MESSAGE_KEY, new object[] { resourceType.Name, method.Name, oslcPropertyDefinition.value })
    {
        this.method = method;
        this.oslcPropertyDefinition = oslcPropertyDefinition;
        this.resourceType = resourceType;
    }

    private static readonly string MESSAGE_KEY = "InvalidPropertyDefinitionException";

    private readonly MethodInfo method;
    private readonly OslcPropertyDefinition oslcPropertyDefinition;
    private readonly Type resourceType;
}
