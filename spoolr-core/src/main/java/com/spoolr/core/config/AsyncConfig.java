package com.spoolr.core.config;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.scheduling.annotation.EnableAsync;
import org.springframework.scheduling.concurrent.ThreadPoolTaskExecutor;

import java.util.concurrent.Executor;

/**
 * 🚀 Async Configuration for PrintWave
 * 
 * This configuration fixes the TaskExecutor issue that was preventing
 * email notifications from being sent asynchronously.
 * 
 * The error was: "More than one TaskExecutor bean found within the context, 
 * and none is named 'taskExecutor'"
 */
@Configuration
@EnableAsync
public class AsyncConfig {
    
    /**
     * 📧 Primary TaskExecutor for async email operations
     * 
     * This bean is specifically named 'taskExecutor' as required by Spring's
     * @Async annotation processing. This will resolve the TaskExecutor conflict
     * and enable proper async email sending.
     */
    @Bean(name = "taskExecutor")
    public Executor taskExecutor() {
        ThreadPoolTaskExecutor executor = new ThreadPoolTaskExecutor();
        
        // Core pool size: number of threads to keep in pool, even if idle
        executor.setCorePoolSize(5);
        
        // Maximum pool size: maximum number of threads to allow in pool
        executor.setMaxPoolSize(10);
        
        // Queue capacity: queue to use for holding tasks before they are executed
        executor.setQueueCapacity(100);
        
        // Thread name prefix: helps with debugging logs
        executor.setThreadNamePrefix("PrintWave-Async-");
        
        // Rejection policy: what to do when queue is full and max pool size reached
        executor.setRejectedExecutionHandler(new java.util.concurrent.ThreadPoolExecutor.CallerRunsPolicy());
        
        // Wait for scheduled tasks to complete on shutdown
        executor.setWaitForTasksToCompleteOnShutdown(true);
        executor.setAwaitTerminationSeconds(60);
        
        // Initialize the executor
        executor.initialize();
        
        return executor;
    }
    
    /**
     * 📧 Dedicated TaskExecutor for email operations (alternative name)
     * 
     * This provides an alternative bean name in case the primary one conflicts
     * with other configurations.
     */
    @Bean(name = "emailTaskExecutor")
    public Executor emailTaskExecutor() {
        ThreadPoolTaskExecutor executor = new ThreadPoolTaskExecutor();
        executor.setCorePoolSize(3);
        executor.setMaxPoolSize(5);
        executor.setQueueCapacity(50);
        executor.setThreadNamePrefix("PrintWave-Email-");
        executor.setRejectedExecutionHandler(new java.util.concurrent.ThreadPoolExecutor.CallerRunsPolicy());
        executor.setWaitForTasksToCompleteOnShutdown(true);
        executor.setAwaitTerminationSeconds(30);
        executor.initialize();
        return executor;
    }
}
