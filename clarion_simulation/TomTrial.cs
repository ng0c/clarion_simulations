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
    public class TomTrial
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
            StreamWriter sw = File.CreateText("TomTrial.txt");

            //Describes the simulation environment
            DimensionValuePair box = World.NewDimensionValuePair("Item", "Box");
            DimensionValuePair basket = World.NewDimensionValuePair("Item", "Basket");
            DimensionValuePair marbles = World.NewDimensionValuePair("Item", "Marbles");
            DimensionValuePair location = World.NewDimensionValuePair("Marbles", "Box");
            DimensionValuePair sayWhat = World.NewDimensionValuePair("YourAction", "Where are the marbles?");


            //Specifies external actions that the agent can perform
            ExternalActionChunk sayBox = World.NewExternalActionChunk("Box");
            ExternalActionChunk sayBasket = World.NewExternalActionChunk("Basket");

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
            net.Input.Add(box);
            net.Input.Add(basket);
            net.Input.Add(marbles);
            net.Input.Add(location);
            net.Input.Add(sayWhat);

            net.Output.Add(sayBox);
            net.Output.Add(sayBasket);

            //Once all specifications are finished. We MUST use Commit.
            //This will allow our agent to starting using the sensory information and actions we have specified.
            John.Commit(net);

            //Parameters to optimise the agent's performance
            net.Parameters.LEARNING_RATE = 1;
            John.ACS.Parameters.PERFORM_RER_REFINEMENT = false;

            //Run the task
            Console.WriteLine("Running the Simple Hello World Task");
            Console.SetOut(sw);

            //Sensory information pointer hold onto the sensory information for the current perception-action cycle
            SensoryInformation si;

            //chosen captures the action chosen by the agent
            ExternalActionChunk chosen;

            for (int i = 0; i < NumberTrials; i++)
            {
                //We obtain sensory information objects from the world by calling the
                //NewSensoryInformation method and specifying the agent for whom the sensory information is intended.
                si = World.NewSensoryInformation(John);
              
                //Ask where the marbles are
                si.Add(sayWhat, John.Parameters.MAX_ACTIVATION);
              
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
                if (chosen == sayBox)
                {
                    //The agent said "Hello".
                    if (si[sayWhat] == John.Parameters.MAX_ACTIVATION)
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

                Console.SetOut(orig);
                progress = (int)(((double)(i + 1) / (double)NumberTrials) * 100);
                Console.CursorLeft = 0;
                Console.Write(progress + "% Complete..");
                Console.SetOut(sw);
            }

            //Report Results

            Console.WriteLine("Reporting Results for the False-belief Task");
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
            Console.WriteLine("Killing Agent to end the program");
            John.Die();
            Console.WriteLine("Agent is Dead");

            Console.WriteLine("The Simple Hello World Task has finished");
            Console.WriteLine("The results have been saved to \"TomTrial.txt\"");
            Console.Write("Press any key to exit");
            Console.ReadKey(true);
        }
    }
}