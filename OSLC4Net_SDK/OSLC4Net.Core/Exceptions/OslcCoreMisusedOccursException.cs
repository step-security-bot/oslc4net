﻿/*******************************************************************************
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

namespace OSLC4Net.Core.Exceptions;

/// <summary>
/// Exception thrown for an incorrect use of the OSLC Occurs attribute
/// </summary>
public class OslcCoreMisusedOccursException : OslcCoreApplicationException
{

    /// <summary>
    ///
    /// </summary>
    /// <param name="resourceType"></param>
    /// <param name="method"></param>
    public OslcCoreMisusedOccursException(Type resourceType, MethodInfo method) :
        base(MESSAGE_KEY, new object[] {resourceType.Name, method.Name})
    {
        this.method        = method;
        this.resourceType = resourceType;
    }

	    public MethodInfo GetMethod() {
        return method;
    }

    public Type GetResourceType() {
        return resourceType;
    }

    private static readonly string MESSAGE_KEY = "MisusedOccursException";

    private MethodInfo     method;
    private Type   resourceType;
}
