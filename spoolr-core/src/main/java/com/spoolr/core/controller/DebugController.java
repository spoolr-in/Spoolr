package com.spoolr.core.controller;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.context.ApplicationContext;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.Arrays;
import java.util.Map;
import java.util.stream.Collectors;

@RestController
public class DebugController {

    @Autowired
    private ApplicationContext applicationContext;

    @GetMapping("/debug/beans")
    public Map<String, String> getBeanNames() {
        return Arrays.stream(applicationContext.getBeanDefinitionNames())
                .collect(Collectors.toMap(name -> name, name -> applicationContext.getBean(name).getClass().getName()));
    }
}
