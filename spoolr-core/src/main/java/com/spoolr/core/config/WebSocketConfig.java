package com.spoolr.core.config;

import org.springframework.context.annotation.Configuration;
import org.springframework.messaging.simp.config.MessageBrokerRegistry;
import org.springframework.web.socket.config.annotation.EnableWebSocketMessageBroker;
import org.springframework.web.socket.config.annotation.StompEndpointRegistry;
import org.springframework.web.socket.config.annotation.WebSocketMessageBrokerConfigurer;

/**
 * WebSocketConfig - Configures the WebSocket message broker for real-time communication.
 *
 * This class sets up the necessary components for enabling WebSocket-based
 * communication between the Spring Boot backend and client applications (like the Station App).
 *
 * It defines:
 * 1. The connection endpoint that clients use to establish a WebSocket connection.
 * 2. The message prefixes for routing messages between clients and the server.
 */
@Configuration
@EnableWebSocketMessageBroker
public class WebSocketConfig implements WebSocketMessageBrokerConfigurer {

    /**
     * Registers the STOMP endpoints, mapping each to a specific URL and enabling SockJS fallback options.
     *
     * @param registry The registry to add the endpoint to.
     */
    @Override
    public void registerStompEndpoints(StompEndpointRegistry registry) {
        // Raw WebSocket endpoint for STOMP protocol
        // Station App (C# ClientWebSocket) connects directly without SockJS wrapper
        // Modern browsers also support raw WebSocket without fallback needed
        registry.addEndpoint("/ws")
                .setAllowedOrigins("*");
    }

    /**
     * Configures the message broker, which is responsible for routing messages
     * from one client to another.
     *
     * @param registry The message broker registry.
     */
    @Override
    public void configureMessageBroker(MessageBrokerRegistry registry) {
        // This sets up a simple in-memory message broker.
        // It defines the prefixes for destinations that the server will send messages to.
        // Clients will subscribe to destinations starting with "/topic" or "/queue".
        // - "/topic" is typically used for publish-subscribe (one-to-many) communication.
        // - "/queue" is typically used for point-to-point (one-to-one) messaging.
        // We will use "/queue" to send private job offers to specific vendors.
        registry.enableSimpleBroker("/topic", "/queue");

        // This defines the prefix for messages that are bound for methods annotated with @MessageMapping.
        // For example, if a client sends a message to a destination like "/app/process-job",
        // it will be routed to a controller method annotated with @MessageMapping("/process-job").
        registry.setApplicationDestinationPrefixes("/app");
    }
}
