using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;
using Clarion;
using Clarion.Framework;

namespace Clarion.Samples
{
    public class HelloWorldSimple
    {
        static void Main(string[] args)
        {
            //Initialize the task
            Console.WriteLine("Initializing the Simple Hello World Task");

            int CorrectCounter = 0;
            int NumberTrials = 10000;
            int progress = 0;

            //Allows to track inner workings of the agent. Can be turned on/off
            //See 'useful features' guide for more information
            World.LoggingLevel = TraceLevel.Off;

            //Write the output to a txt file instead of the console
            //This is not a requirement
            TextWriter orig = Console.Out;
            StreamWriter sw = File.CreateText("HelloWorldSimple.txt");

            //Describes the simulation environment
            DimensionValuePair hi = World.NewDimensionValuePair("Salutation", "Hello");
            DimensionValuePair bye = World.NewDimensionValuePair("Salutation", "Goodbye");

            //Specifies external actions that the agent can perform
            ExternalActionChunk sayHi = World.NewExternalActionChunk("Hello");
            ExternalActionChunk sayBye = World.NewExternalActionChunk("Goodbye");

            //Initialize the Agent
            //The label is optional but useful if we later want to retrieve
            //it from World using Get.
            //This agent is currently "empty" it does not know anything about the world
            Agent John = World.NewAgent("John");

            //Initialise agent with Implicit Decision Network (IDN) in the bottom level of ACS
            //AgentInitializer is an important object. This is how we attach
            //implicit components, meta-cognitive modules etc. to our agent.
            SimplifiedQBPNetwork net = AgentInitializer.InitializeImplicitDecisionNetwork(John, SimplifiedQBPNetwork.Factory);

            //Further initialises our IDN
            //This will give our agent the ability to choose actions based on the sensory
            //information it receives from the world.
            net.Input.Add(hi);
            net.Input.Add(bye);

            net.Output.Add(sayHi);
            net.Output.Add(sayBye);

            //Once all specifications are finished. We MUST use Commit.
            //This will allow our agent to starting using the sensory information and actions we have specified.
            John.Commit(net);

            //Parameters to optimise the agent's performance
            net.Parameters.LEARNING_RATE = 1;
            John.ACS.Parameters.PERFORM_RER_REFINEMENT = false;

            //Run the task
            Console.WriteLine("Running the Simple Hello World Task");
            Console.SetOut(sw);

            //rand - randomly choosing the configuration of the sensory info
            Random rand = new Random();
            //Sensory information pointer hold onto the sensory information for the current perception-action cycle
            SensoryInformation si;

            //chosen captures the action chosen by the agent
            ExternalActionChunk chosen;

            for (int i = 0; i < NumberTrials; i++)
            {
                //We obtain sensory information objects from the world by calling the
                //NewSensoryInformation method and specifying the agent for whom the sensory information is intended.
                si = World.NewSensoryInformation(John);

                //Randomly choose an input to perceive.
                if (rand.NextDouble() < .5)
                {
                    //Say "Hello"
                    si.Add(hi, John.Parameters.MAX_ACTIVATION);
                    si.Add(bye, John.Parameters.MIN_ACTIVATION);
                }
                else
                {
                    //Say "Goodbye"
                    si.Add(hi, John.Parameters.MIN_ACTIVATION);
                    si.Add(bye, John.Parameters.MAX_ACTIVATION);
                }

                //Perceive the sensory information
                //The Perceive method initiates the process of decision-making
                John.Perceive(si);

                //Choose an action
                //GetChosenExternalAction returns the action that is chosen by John
                //(given the current sensory information)
                chosen = John.GetChosenExternalAction(si);

                //Deliver appropriate feedback to the agent, either reward or punishment
                //notice that we are able to compare our actions and sensory information
                //objects using the standard “==” comparator
                if (chosen == sayHi)
                {
                    //The agent said "Hello".
                    if (si[hi] == John.Parameters.MAX_ACTIVATION)
                    {
                        //The agent responded correctly
                        Trace.WriteLineIf(World.LoggingSwitch.TraceWarning, "John was correct");
                        //Record the agent's success.
                        CorrectCounter++;
                        //Give positive feedback.
                        //To give our agent feedback, all we need to do is call the ReceiveFeedback method.
                        //Calling this method automatically initiates a round of learning inside John.
                        //Feedback in this case is 0-1 but it does not need to be. See the “Intermediate ACS Setup” for
                        //additional considerations regarding this.
                        John.ReceiveFeedback(si, 1.0);
                    }
                    else
                    {
                        //The agent responded incorrectly
                        Trace.WriteLineIf(World.LoggingSwitch.TraceWarning, "John was incorrect");
                        //Give negative feedback.
                        John.ReceiveFeedback(si, 0.0);
                    }
                }
                else
                {
                    //The agent said "Goodbye".
                    if (si[bye] == John.Parameters.MAX_ACTIVATION)
                    {
                        //The agent responded correctly
                        Trace.WriteLineIf(World.LoggingSwitch.TraceWarning, "John was correct");
                        //Record the agent's success.
                        CorrectCounter++;
                        //Give positive feedback.
                        John.ReceiveFeedback(si, 1.0);
                    }
                    else
                    {
                        //The agent responded incorrectly
                        Trace.WriteLineIf(World.LoggingSwitch.TraceWarning, "John was incorrect");
                        //Give negative feedback.
                        John.ReceiveFeedback(si, 0.0);
                    }
                }

                Console.SetOut(orig);
                progress = (int)(((double)(i+1) / (double)NumberTrials) * 100);
                Console.CursorLeft = 0;
                Console.Write(progress + "% Complete..");
                Console.SetOut(sw);
            }

            //Report Results

            Console.WriteLine("Reporting Results for the Simple Hello World Task");
            Console.WriteLine("John got " + CorrectCounter + " correct out of " + NumberTrials + " trials (" +
                (int)Math.Round(((double)CorrectCounter / (double)NumberTrials) * 100) + "%)");

            Console.WriteLine("At the end of the task, John had learned the following rules:");
            foreach (var i in John.GetInternals(Agent.InternalContainers.ACTION_RULES))
                Console.WriteLine(i);

            sw.Close();
            Console.SetOut(orig);
            Console.CursorLeft = 0;
            Console.WriteLine("100% Complete..");
            //Kill the agent to end the task
            Console.WriteLine("Killing John to end the program");
            John.Die();
            Console.WriteLine("John is Dead");

            Console.WriteLine("The Simple Hello World Task has finished");
            Console.WriteLine("The results have been saved to \"HelloWorldSimple.txt\"");
            Console.Write("Press any key to exit");
            Console.ReadKey(true);
        }
    }
}