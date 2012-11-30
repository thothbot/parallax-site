/*
 * Copyright 2012 Alex Usachev, thothbot@gmail.com
 * 
 * This file is part of Parallax project.
 * 
 * Parallax is free software: you can redistribute it and/or modify it 
 * under the terms of the Creative Commons Attribution 3.0 Unported License.
 * 
 * Parallax is distributed in the hope that it will be useful, but 
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
 * or FITNESS FOR A PARTICULAR PURPOSE. See the Creative Commons Attribution 
 * 3.0 Unported License. for more details.
 * 
 * You should have received a copy of the the Creative Commons Attribution 
 * 3.0 Unported License along with Parallax. 
 * If not, see http://creativecommons.org/licenses/by/3.0/.
 */

package thothbot.parallax.site.client;

import thothbot.parallax.core.client.AnimatedScene;
import thothbot.parallax.core.client.RenderingPanel;
import thothbot.parallax.core.client.events.AnimationReadyEvent;
import thothbot.parallax.core.client.events.AnimationReadyHandler;
import thothbot.parallax.core.client.events.Context3dErrorEvent;
import thothbot.parallax.core.client.events.Context3dErrorHandler;
import thothbot.parallax.core.client.events.SceneLoadingEvent;
import thothbot.parallax.core.client.events.SceneLoadingHandler;

import com.google.gwt.core.client.EntryPoint;
import com.google.gwt.core.client.GWT;
import com.google.gwt.core.client.Scheduler;
import com.google.gwt.event.dom.client.ClickEvent;
import com.google.gwt.event.dom.client.ClickHandler;
import com.google.gwt.event.logical.shared.ResizeEvent;
import com.google.gwt.event.logical.shared.ResizeHandler;
import com.google.gwt.resources.client.ClientBundle;
import com.google.gwt.resources.client.ImageResource;
import com.google.gwt.user.client.Window;
import com.google.gwt.user.client.ui.FlowPanel;
import com.google.gwt.user.client.ui.Image;
import com.google.gwt.user.client.ui.Label;
import com.google.gwt.user.client.ui.RootPanel;
import com.google.gwt.user.client.ui.ToggleButton;

/**
 * Entry point classes define <code>onModuleLoad()</code>.
 */
public class Site implements EntryPoint, 
	AnimationReadyHandler, SceneLoadingHandler, Context3dErrorHandler, ResizeHandler
{
	public interface SiteResources extends ClientBundle
	{
		ImageResource play();
		ImageResource pause();
		ImageResource stop();
	}
	
	/**
	 * The static resources used throughout the Demo.
	 */
	public static final SiteResources resources = GWT.create(SiteResources.class);
	private RenderingPanel renderingPanel;
	
	FlowPanel infoPanel;
	private ToggleButton animationSwitch;
	private Label infoText;
	
	/**
	 * This is the entry point method.
	 */
	public void onModuleLoad() 
	{			  
		infoText = new Label("Loading...");
		infoText.setStyleName("info");
		animationSwitch = new ToggleButton();
		animationSwitch.setEnabled(false);
		animationSwitch.getUpFace().setImage(new Image(resources.stop()));

		TerrainDynamic scene = new TerrainDynamic();
		renderingPanel = new RenderingPanel();
		RootPanel.get("animation").add(renderingPanel);

		// Background color
		renderingPanel.setBackground(0x111111);
		renderingPanel.addAnimationReadyHandler(this);
		renderingPanel.addSceneLoadingHandler(this);
		renderingPanel.addCanvas3dErrorHandler(this);

		renderingPanel.setAnimatedScene(scene.getDemoScene());
		
		infoPanel = new FlowPanel();
		RootPanel.get("animationPanel").add(infoPanel);
		infoPanel.add(animationSwitch);
		infoPanel.add(infoText);
	}
	
	/**
	 * This event called when {@link RenderingPanel} is ready to animate a 
	 * {@link AnimatedScene} in loaded example.
	 */
	public void onAnimationReady(AnimationReadyEvent event)
	{
		animationSwitch.setEnabled(true);
		animationSwitch.setDown(true);
		animationSwitch.getUpFace().setImage(new Image(resources.pause()));
		animationSwitch.addClickHandler(new ClickHandler() {
    		public void onClick(ClickEvent event) {
    			if (animationSwitch.isDown())
    			{
    				animationSwitch.getUpFace().setImage(new Image(resources.pause()));
    				Site.this.renderingPanel.getAnimatedScene().run();
    			}
    			else
    			{
    				animationSwitch.getUpFace().setImage(new Image(resources.play()));
    				Site.this.renderingPanel.getAnimatedScene().stop();
    			}
    		}
    	});
	}

	@Override
	public void onSceneLoading(SceneLoadingEvent event) 
	{
		if(event.isLoaded())
		{
			infoText.setText("Your browser supports WebGL!");	
			Window.addResizeHandler(this);
		}
		else
		{
			infoText.setText("Loading scene...");
		}
	}

	@Override
	public void onContextError(Context3dErrorEvent event) 
	{
		infoText.setText("Your browser does not support WebGL.");
		RootPanel.get("animation").remove(renderingPanel);
	}
	
	@Override
	public void onResize(ResizeEvent event) 
	{
		Scheduler.get().scheduleDeferred(new Scheduler.ScheduledCommand() {
			@Override
			public void execute() 
			{
				renderingPanel.onResize();
			}
		});
	}
}
