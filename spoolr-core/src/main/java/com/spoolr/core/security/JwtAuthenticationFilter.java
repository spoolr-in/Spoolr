package com.spoolr.core.security;

import com.spoolr.core.util.JwtUtil;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.authority.SimpleGrantedAuthority;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.web.authentication.WebAuthenticationDetailsSource;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

import java.io.IOException;
import java.util.Collections;

@Component
public class JwtAuthenticationFilter extends OncePerRequestFilter {

    @Autowired
    private JwtUtil jwtUtil;

    @Override
    protected void doFilterInternal(HttpServletRequest request, 
                                  HttpServletResponse response, 
                                  FilterChain filterChain) throws ServletException, IOException {
        
        // Log all incoming requests
        System.out.println("=== INCOMING REQUEST ===");
        System.out.println("Method: " + request.getMethod());
        System.out.println("URI: " + request.getRequestURI());
        System.out.println("Remote: " + request.getRemoteAddr());
        
        // Extract JWT token from Authorization header
        final String authorizationHeader = request.getHeader("Authorization");
        System.out.println("Authorization header: " + (authorizationHeader != null ? "Present (length: " + authorizationHeader.length() + ")" : "Missing"));
        
        String email = null;
        String jwt = null;
        
        // Check if Authorization header exists and starts with "Bearer "
        if (authorizationHeader != null && authorizationHeader.startsWith("Bearer ")) {
            jwt = authorizationHeader.substring(7); // Remove "Bearer " prefix
            try {
                email = jwtUtil.extractEmail(jwt);
                System.out.println("JWT token extracted email: " + email);
            } catch (Exception e) {
                // Token is invalid or expired
                System.out.println("JWT token validation failed: " + e.getMessage());
                logger.error("JWT token validation failed: " + e.getMessage());
            }
        }
        
        // If we have a valid email and no authentication is set yet
        if (email != null && SecurityContextHolder.getContext().getAuthentication() == null) {
            
            // Validate the token
            if (jwtUtil.validateToken(jwt, email)) {
                
                // Extract user information from token
                String role = jwtUtil.extractRole(jwt);
                Long userId = jwtUtil.extractUserId(jwt);
                
                // Create authentication token with role
                SimpleGrantedAuthority authority = new SimpleGrantedAuthority("ROLE_" + role);
                UsernamePasswordAuthenticationToken authToken = 
                    new UsernamePasswordAuthenticationToken(
                        email, 
                        null, 
                        Collections.singletonList(authority)
                    );
                
                // Set additional details
                authToken.setDetails(new WebAuthenticationDetailsSource().buildDetails(request));
                
                // Set authentication in security context
                SecurityContextHolder.getContext().setAuthentication(authToken);
                
                // Add user info to request attributes for easy access
                request.setAttribute("userId", userId);
                request.setAttribute("userEmail", email);
                request.setAttribute("userRole", role);
            }
        }
        
        // Continue with the filter chain
        filterChain.doFilter(request, response);
    }

    @Override
    protected boolean shouldNotFilter(HttpServletRequest request) throws ServletException {
        return request.getRequestURI().startsWith("/ws");
    }
}
