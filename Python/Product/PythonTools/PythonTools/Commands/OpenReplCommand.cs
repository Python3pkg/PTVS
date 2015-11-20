// Python Tools for Visual Studio
// Copyright(c) Microsoft Corporation
// All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the License); you may not use
// this file except in compliance with the License. You may obtain a copy of the
// License at http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS
// OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY
// IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABLITY OR NON-INFRINGEMENT.
//
// See the Apache Version 2.0 License for specific language governing
// permissions and limitations under the License.

using System;
using System.Linq;
using Microsoft.PythonTools.Interpreter;
using Microsoft.PythonTools.Project;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudioTools;

namespace Microsoft.PythonTools.Commands {
    /// <summary>
    /// Provides the command for starting the Python REPL window.
    /// </summary>
    class OpenReplCommand : Command {
        private readonly IServiceProvider _serviceProvider;

        public OpenReplCommand(IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider;
        }

        public override void DoCommand(object sender, EventArgs e) {
            // Use the factory or command line passed as an argument.
            IPythonInterpreterFactory factory = null;
            var oe = e as OleMenuCmdEventArgs;
            if (oe != null) {
                string args;
                if ((factory = oe.InValue as IPythonInterpreterFactory) == null &&
                    !string.IsNullOrEmpty(args = oe.InValue as string)
                ) {
                    string description;
                    var parse = _serviceProvider.GetService(typeof(SVsParseCommandLine)) as IVsParseCommandLine;
                    if (ErrorHandler.Succeeded(parse.ParseCommandTail(args, -1)) &&
                        ErrorHandler.Succeeded(parse.EvaluateSwitches("e,env,environment:")) &&
                        ErrorHandler.Succeeded(parse.GetSwitchValue(0, out description)) &&
                        !string.IsNullOrEmpty(description)
                    ) {
                        var service = _serviceProvider.GetComponentModel().GetService<IInterpreterOptionsService>();
                        factory = service.Interpreters.FirstOrDefault(
                            // Descriptions are localized strings, hence CCIC
                            f => description.Equals(f.Description, StringComparison.CurrentCultureIgnoreCase)
                        );
                    }
                }
            }

            if (factory == null) {
                var service = _serviceProvider.GetComponentModel().GetService<IInterpreterOptionsService>();
                factory = service.DefaultInterpreter;
            }

            // This command is project-insensitive
            var provider = _serviceProvider.GetComponentModel()?.GetService<Repl.InteractiveWindowProvider>();
            try {
                provider?.OpenOrCreate(
                    factory != null ? Repl.PythonReplEvaluatorProvider.GetEvaluatorId(factory) : null
                );
            } catch (Exception ex) when (!ex.IsCriticalException()) {
                throw new InvalidOperationException(SR.GetString(SR.ErrorOpeningInteractiveWindow, ex));
            }
        }
        
        public override int CommandId {
            get { return (int)PkgCmdIDList.cmdidReplWindow; }
        }
    }
}
