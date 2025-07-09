package com.printwave.core;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.scheduling.annotation.EnableAsync;

@SpringBootApplication
@EnableAsync
public class PrintwaveCoreApplication {

	public static void main(String[] args) {
		SpringApplication.run(PrintwaveCoreApplication.class, args);
	}

}
