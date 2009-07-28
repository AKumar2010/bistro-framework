﻿/****************************************************************************
 * 
 *  Bistro Framework Copyright © 2003-2009 Hill30 Inc
 *
 *  This file is part of Bistro Framework.
 *
 *  Bistro Framework is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Lesser General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Bistro Framework is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with Bistro Framework.  If not, see <http://www.gnu.org/licenses/>.
 *  
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bistro.Controllers.Descriptor;
using Bistro.Controllers.OutputHandling;

namespace Bistro.Controllers
{
    /// <summary>
    /// Default handler factory implementation
    /// </summary>
    public class HandlerFactory: IControllerHandlerFactory
    {
        private Application application;

        /// <summary>
        /// Initializes a new instance of the <see cref="HandlerFactory"/> class.
        /// </summary>
        /// <param name="application">The application.</param>
        public HandlerFactory(Application application)
        {
            this.application = application;
        }

        /// <summary>
        /// Creates the controller handler for the given descriptor.
        /// </summary>
        /// <param name="descriptor">The descriptor.</param>
        /// <returns></returns>
        public IControllerHandler CreateControllerHandler(ControllerDescriptor descriptor)
        {
#warning Trying to remove ControllerHandler
//            return new ControllerHandler(descriptor, application.LoggerFactory.GetLogger(typeof(ControllerHandler)));
            return null;
        }
    }
}