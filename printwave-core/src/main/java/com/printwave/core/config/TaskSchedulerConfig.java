package com.printwave.core.config;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.scheduling.TaskScheduler;
import org.springframework.scheduling.concurrent.ThreadPoolTaskScheduler;

/**
 * TaskSchedulerConfig - Configuration for TaskScheduler bean
 * 
 * This config ensures that a TaskScheduler bean is available for 
 * PrintJobService to use for automatic job status progression.
 */
@Configuration
public class TaskSchedulerConfig {
    
    /**
     * Create TaskScheduler bean for scheduling delayed tasks
     * Used for vendor timeout handling and automatic status progression
     */
    @Bean
    public TaskScheduler taskScheduler() {
        ThreadPoolTaskScheduler scheduler = new ThreadPoolTaskScheduler();
        scheduler.setPoolSize(10);
        scheduler.setThreadNamePrefix("PrintWave-Scheduler-");
        scheduler.setWaitForTasksToCompleteOnShutdown(true);
        scheduler.setAwaitTerminationSeconds(30);
        return scheduler;
    }
}
