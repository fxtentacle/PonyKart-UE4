﻿using System;
using System.Collections.ObjectModel;
using BulletSharp;
using Mogre;
using Ponykart.Levels;
using Ponykart.Lua;
using Ponykart.Physics;
using PonykartParsers;

namespace Ponykart.Actors {

	public class LThing : LDisposable {
		public int ID { get; protected set; }
		public string Name { get; protected set; }
		public RigidBody Body { get; protected set; }
		public SceneNode RootNode { get; protected set; }
		protected StaticGeometry StaticGeometry { get; set; }

		/// <summary>
		/// Initial motion state setter. Override this if you want something different. This is only used for initialisation!
		/// </summary>
		protected virtual MotionState DefaultMotionState { get { return new MogreMotionState(SpawnPosition, SpawnOrientation, RootNode); } }
		/// <summary>
		/// The actual motion state.
		/// </summary>
		protected MotionState MotionState { get; set; }
		/// <summary>
		/// The body's collision group
		/// </summary>
		public PonykartCollisionGroups CollisionGroup { get; protected set; }
		/// <summary>
		/// What does the body collide with?
		/// </summary>
		public PonykartCollidesWithGroups CollidesWith { get; protected set; }


		/// <summary>
		/// The thing's initial position when it's first created
		/// </summary>
		public Vector3 SpawnPosition { get; private set; }
		/// <summary>
		/// The thing's initial orientation when it's first created
		/// </summary>
		public Quaternion SpawnOrientation { get; private set; }
		/// <summary>
		/// The thing's initial scale when it's first created. This is only used if it doesn't have any shape components.
		/// </summary>
		public Vector3 SpawnScale { get; private set; }

		protected RigidBodyConstructionInfo Info;
		protected string Script;

		public Collection<ModelComponent> ModelComponents { get; protected set; }
		public Collection<ShapeComponent> ShapeComponents { get; protected set; }
		public Collection<RibbonComponent> RibbonComponents { get; protected set; }
		public Collection<BillboardSetComponent> BillboardSetComponents { get; protected set; }

		public LThing(ThingBlock template, ThingDefinition def) {
			ID = IDs.New;
			Name = template.ThingName;

			ModelComponents = new Collection<ModelComponent>();
			ShapeComponents = new Collection<ShapeComponent>();
			RibbonComponents = new Collection<RibbonComponent>();
			BillboardSetComponents = new Collection<BillboardSetComponent>();

			// get our three basic transforms
			SpawnPosition = template.GetVectorProperty("position", null);

			SpawnOrientation = template.GetQuatProperty("orientation", Quaternion.IDENTITY);
			if (SpawnOrientation == Quaternion.IDENTITY)
				SpawnOrientation = template.GetVectorProperty("rotation", Vector3.ZERO).DegreeVectorToGlobalQuaternion();

			SpawnScale = template.GetVectorProperty("scale", Vector3.UNIT_SCALE);


			// and start setting up this thing!
			Setup(template, def);
			SetupMogre(template, def);
			InitialiseComponents(template, def);

			RootNode.Position = SpawnPosition;
			RootNode.Orientation = SpawnOrientation;
			// only scale up the root node if it doesn't have any physics things attached - bullet really does not like scaling.
			// Need a few variations of identical objects with different scales? Gonna have to make different .things for them.
			// Though it might be easier to just have one general .thing for them, and all it does is run a script that randomly
			// gets one of the others.
			if (ShapeComponents.Count == 0)
				RootNode.Scale(SpawnScale);
			RootNode.SetInitialState();

			SetupStaticGeom(template, def);

			SetupPhysics(template, def);

			// get our script token and run it, if it has one and if this thing was created on the fly instead
			// of through a .muffin file
			if (def.StringTokens.TryGetValue("script", out Script)) {
				if (LKernel.GetG<LevelManager>().IsValidLevel)
					RunScript();
			}
		}

		/// <summary>
		/// Use this method if you need some more stuff to happen before the constructor starts setting everything up.
		/// For example if you need to get more things out of the ThingTemplate, you can use this for that.
		/// </summary>
		protected virtual void Setup(ThingBlock template, ThingDefinition def) { }

		/// <summary>
		/// Sets up mogre stuff, like our root scene node
		/// </summary>
		protected void SetupMogre(ThingBlock template, ThingDefinition def) {
			// create our root node
			RootNode = LKernel.GetG<SceneManager>().RootSceneNode.CreateChildSceneNode(Name + ID);
		}

		/// <summary>
		/// Make our components
		/// </summary>
		protected void InitialiseComponents(ThingBlock template, ThingDefinition def) {
			// ogre stuff
			foreach (var mblock in def.ModelBlocks)
				ModelComponents.Add(new ModelComponent(this, template, mblock));
			// bullet stuff
			foreach (var sblock in def.ShapeBlocks)
				ShapeComponents.Add(new ShapeComponent(this, template, sblock));
			// ribbons
			foreach (var rblock in def.RibbonBlocks)
				RibbonComponents.Add(new RibbonComponent(this, template, rblock));
			// billboard sets
			foreach (var bblock in def.BillboardSetBlocks)
				BillboardSetComponents.Add(new BillboardSetComponent(this, template, bblock));
		}

		/// <summary>
		/// Set up static geometry if we have some
		/// </summary>
		protected void SetupStaticGeom(ThingBlock template, ThingDefinition def) {
			if (def.GetBoolProperty("Static", false)) {
				// create it
				StaticGeometry = LKernel.GetG<SceneManager>().CreateStaticGeometry(Name + ID);
				StaticGeometry.Origin = SpawnPosition;
				// add all of our meshes and stuff
				StaticGeometry.AddSceneNode(RootNode);
				// once you do this, you can't add any new geometry to it
				StaticGeometry.Build();

				// since now we have two copies of the same geometry, we want to get rid of the old stuff
				foreach (ModelComponent mc in ModelComponents)
					mc.Dispose();
			}
		}

		protected void SetupPhysics(ThingBlock template, ThingDefinition def) {
			// if we have no shape components then we don't set up physics
			if (ShapeComponents.Count == 0)
				return;

			PreSetUpBodyInfo();
			SetUpBodyInfo(def);
			PostSetUpBodyInfo();
			CreateBody(def);
			PostCreateBody(def);
			SetBodyUserData();
		}

		/// <summary>
		/// If you need anything before we set up the body info
		/// </summary>
		protected virtual void PreSetUpBodyInfo() { }

		/// <summary>
		/// Set up all of the stuff needed before we create our body
		/// </summary>
		protected void SetUpBodyInfo(ThingDefinition def) {
			// set up our collision shapes
			CollisionShape shape;
			// if we just have one shape component, we use its shape as the main one
			if (ShapeComponents.Count == 1) {
				shape = ShapeComponents[0].Shape;
			}
			// if we have more than one component we'll need a compound shape
			else {
				CompoundShape comp = new CompoundShape();
				foreach (var sc in ShapeComponents) {
					comp.AddChildShape(sc.Transform, sc.Shape);
				}
				shape = comp;
			}

			// get the physics type and set up the mass of the body
			ThingEnum physicsType = def.GetEnumProperty("physics", null);
			float mass;
			if (physicsType.HasFlag(ThingEnum.Static))
				mass = 0;
			else {
				mass = def.GetFloatProperty("mass", 1);
			}

			// create our construction info thingy
			Vector3 inertia;
			shape.CalculateLocalInertia(mass, out inertia);
			MotionState = DefaultMotionState;
			Info = new RigidBodyConstructionInfo(mass, MotionState, shape, inertia);
			// TODO
			string physmat = def.GetStringProperty("PhysicsMaterial", "Default");
			LKernel.GetG<PhysicsMaterialManager>().ApplyMaterial(Info, physmat);

			// choose which group to use for a default
			ThingEnum defaultGroup;
			if (physicsType.HasFlag(ThingEnum.Dynamic))
				defaultGroup = ThingEnum.Default;
			else if (physicsType.HasFlag(ThingEnum.Static))
				defaultGroup = ThingEnum.Environment;
			else // kinematic
				defaultGroup = ThingEnum.Walls;

			// collision group
			ThingEnum collisionGroup = def.GetEnumProperty("CollisionGroup", defaultGroup);
			PonykartCollisionGroups pcg;
			if (!Enum.TryParse<PonykartCollisionGroups>(collisionGroup + String.Empty, true, out pcg))
				throw new FormatException("Invalid collision group!");
			CollisionGroup = pcg;

			// collides-with group
			ThingEnum collidesWith = def.GetEnumProperty("CollidesWith", defaultGroup);
			PonykartCollidesWithGroups pcwg;
			if (!Enum.TryParse<PonykartCollidesWithGroups>(collidesWith + String.Empty, true, out pcwg))
				throw new FormatException("Invalid collides-with group!");
			CollidesWith = pcwg;

			// update the transforms
			Matrix4 transform = new Matrix4();
			transform.MakeTransform(SpawnPosition, SpawnScale, SpawnOrientation);
			Info.StartWorldTransform = transform;
			MotionState.WorldTransform = transform;
		}

		/// <summary>
		/// Override this if you want to do more to the construction info before it's used to create the body but after it's been created
		/// </summary>
		protected virtual void PostSetUpBodyInfo() { }

		/// <summary>
		/// Creates the body and makes it static/kinematic if specified.
		/// </summary>
		protected void CreateBody(ThingDefinition def) {
			Body = new RigidBody(Info);

			// stick on our flags
			ThingEnum te = def.GetEnumProperty("physics", null);
			if (te.HasFlag(ThingEnum.Static))
				Body.CollisionFlags |= CollisionFlags.StaticObject;
			else if (te.HasFlag(ThingEnum.Kinematic))
				Body.CollisionFlags |= CollisionFlags.KinematicObject;

			Body.WorldTransform = Info.StartWorldTransform;

			LKernel.GetG<PhysicsMain>().World.AddRigidBody(Body, CollisionGroup, CollidesWith);
		}

		/// <summary>
		/// Override this if you want to do more to the rigid body
		/// </summary>
		protected virtual void PostCreateBody(ThingDefinition td) { }

		/// <summary>
		/// Sets the Actor's UserData to this class so we can easily get to it.
		/// </summary>
		protected void SetBodyUserData() {
			Body.UserObject = this;
			Body.SetName(Name);
			Body.SetCollisionGroup(CollisionGroup);
		}

		/// <summary>
		/// Runs the thing's script, if it has one.
		/// If this thing was made from a .muffin, this is called from Level.RunLevelScripts to make sure it runs after everything else is created.
		/// If it was made on the fly, it runs at the end of the constructor, as long as it's a valid level of course.
		/// </summary>
		public void RunScript() {
			if (Script != null)
				LKernel.GetG<LuaMain>().DoFunction(Script, this);
		}

		/// <summary>
		/// clean up
		/// </summary>
		protected override void Dispose(bool disposing) {
			if (IsDisposed)
				return;

			var sceneMgr = LKernel.GetG<SceneManager>();
			var world = LKernel.GetG<PhysicsMain>().World;
			bool valid = LKernel.GetG<LevelManager>().IsValidLevel;

			if (disposing) {
				// dispose all of our components
				foreach (ModelComponent mc in ModelComponents)
					mc.Dispose();
				foreach (ShapeComponent sc in ShapeComponents)
					sc.Dispose();
				foreach (RibbonComponent rc in RibbonComponents)
					rc.Dispose();
				foreach (BillboardSetComponent bb in BillboardSetComponents)
					bb.Dispose();

				// clear our components
				ModelComponents.Clear();
				ShapeComponents.Clear();
				RibbonComponents.Clear();
				BillboardSetComponents.Clear();
			}

			// these are conditional in case we want to dispose stuff in the middle of a level
			if (RootNode != null) {
				if (valid && disposing)
					sceneMgr.DestroySceneNode(RootNode);
				RootNode.Dispose();
				RootNode = null;
			}
			if (Body != null) {
				if (valid && disposing)
					world.RemoveRigidBody(Body);
				Body.Dispose();
				Body = null;
			}
			// we aren't gonna be disposing something static in the middle of a level, so we don't have to tell the scene manager to specifically destroy it,
			// since it'll be removed in the "DestroyAllStaticGeometry()" thing
			if (StaticGeometry != null) {
				StaticGeometry.Dispose();
				StaticGeometry = null;
			}

			base.Dispose(disposing);
		}
	}
}
