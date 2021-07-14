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
        static Agent Participant;
        static int NumberTrials = 10000;
        static int CorrectCounter = 0;
        static int progress = 0;

        static DimensionValuePair sally;
        static DimensionValuePair anne;
        static DimensionValuePair box;
        static DimensionValuePair basket;
        static DimensionValuePair ball;
        static DimensionValuePair location;
        static DimensionValuePair sayWhat;

        static ExternalActionChunk sayBox;
        static ExternalActionChunk sayBasket;

        private static StreamWriter logFile;
        private static TextWriter textWriter;

        static void Main(string[] args)
        {
            //Initialize the task
            Console.WriteLine("Initializing the False-Belief Task");
            InitializeWorld();
            InitializeAgent();
            Run();
            FinishUp();

        }

        public static void InitializeWorld()
        {
            //Allows to track inner workings of the agent. Can be turned on/off
            //See 'useful features' guide for more information
            World.LoggingLevel = TraceLevel.Off;

            //Describes the simulation environment
            sally = World.NewDimensionValuePair("Person", "Sally");
            anne = World.NewDimensionValuePair("Person", "Anne");
            box = World.NewDimensionValuePair("Item", "Box");
            basket = World.NewDimensionValuePair("Item", "Basket");
            ball = World.NewDimensionValuePair("Item", "Ball");
            location = World.NewDimensionValuePair("Ball is in", "Box");
            sayWhat = World.NewDimensionValuePair("YourAction", "Where are the marbles?");


            //Specifies external actions that the agent can perform
            sayBox = World.NewExternalActionChunk("Box");
            sayBasket = World.NewExternalActionChunk("Basket");

        }

        public static void InitializeAgent()
        {
            //Initialize the Agent
            //The label is optional but useful if we later want to retrieve
            //it from World using Get.
            //This agent is currently "empty" it does not know anything about the world
            Participant = World.NewAgent("John");

            //Initialise agent with Implicit Decision Network (IDN) (the most basic reinforcement learning neural network)
            //in the bottom level of ACS.
            //AgentInitializer is an important object. This is how we attach
            //implicit components, meta-cognitive modules etc. to our agent.
            SimplifiedQBPNetwork net = AgentInitializer.InitializeImplicitDecisionNetwork(Participant, SimplifiedQBPNetwork.Factory);

            //Further initialises our IDN
            //This will give our agent the ability to choose actions based on the sensory
            //information it receives from the world.
            net.Input.Add(sally);
            net.Input.Add(anne);
            net.Input.Add(box);
            net.Input.Add(basket);
            net.Input.Add(ball);
            net.Input.Add(location);
            net.Input.Add(sayWhat);

            net.Output.Add(sayBox);
            net.Output.Add(sayBasket);

            //Once all specifications are finished. We MUST use Commit.
            //This will allow our agent to starting using the sensory information and actions we have specified.
            Participant.Commit(net);

            //Parameters to optimise the agent's performance
            net.Parameters.LEARNING_RATE = 1;
            Participant.ACS.Parameters.PERFORM_RER_REFINEMENT = false;
        }

        private static void Run()
        {
            //Write the output to a txt file instead of the console
            //This is not a requirement
            textWriter = Console.Out;
            logFile = File.CreateText("TomTrial2.txt");

            //Run the task
            Console.WriteLine("Running the Simple Hello World Task");
            Console.SetOut(logFile);

            //Sensory information pointer hold onto the sensory information for the current perception-action cycle
            SensoryInformation si;

            //chosen captures the action chosen by the agent
            ExternalActionChunk chosen;

            for (int i = 0; i < NumberTrials; i++)
            {
                //We obtain sensory information objects from the world by calling the
                //NewSensoryInformation method and specifying the agent for whom the sensory information is intended.
                si = World.NewSensoryInformation(Participant);

                //Ask where the marbles are
                si.Add(location, Participant.Parameters.MAX_ACTIVATION);
                si.Add(sayWhat, Participant.Parameters.MAX_ACTIVATION);

                //Perceive the sensory information
                //The Perceive method initiates the process of decision-making
                Participant.Perceive(si);

                //Choose an action
                //GetChosenExternalAction returns the action that is chosen by John
                //(given the current sensory information)
                chosen = Participant.GetChosenExternalAction(si);

                //Deliver appropriate feedback to the agent, either reward or punishment
                //notice that we are able to compare our actions and sensory information
                //objects using the standard “==” comparator
                if (chosen == sayBox)
                {
                    //The agent said "Box".
                    if (si[box] == Participant.Parameters.MAX_ACTIVATION)
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
                        Participant.ReceiveFeedback(si, 1.0);
                    }
                    else
                    {
                        //The agent responded incorrectly
                        Trace.WriteLineIf(World.LoggingSwitch.TraceWarning, "John was incorrect");
                        //Give negative feedback.
                        Participant.ReceiveFeedback(si, 0.0);
                    }
                }

                Console.SetOut(textWriter);
                progress = (int)(((double)(i + 1) / (double)NumberTrials) * 100);
                Console.CursorLeft = 0;
                Console.Write(progress + "% Complete..");
                Console.SetOut(logFile);
            }

        }

        private static void FinishUp()
        {
            //Report Results

            Console.WriteLine("Reporting Results for the False-belief Task");
            Console.WriteLine("John got " + CorrectCounter + " correct out of " + NumberTrials + " trials (" +
                (int)Math.Round(((double)CorrectCounter / (double)NumberTrials) * 100) + "%)");

            Console.WriteLine("At the end of the task, John had learned the following rules:");
            foreach (var i in Participant.GetInternals(Agent.InternalContainers.ACTION_RULES))
                Console.WriteLine(i);

            logFile.Close();
            Console.SetOut(textWriter);
            Console.CursorLeft = 0;
            Console.WriteLine("100% Complete..");
            //Kill the agent to end the task
            Console.WriteLine("Killing Agent to end the program");
            Participant.Die();
            Console.WriteLine("Agent is Dead");

            Console.WriteLine("The Simple Hello World Task has finished");
            Console.WriteLine("The results have been saved to \"TomTrial.txt\"");
            Console.Write("Press any key to exit");
            Console.ReadKey(true);
        }
    }
}